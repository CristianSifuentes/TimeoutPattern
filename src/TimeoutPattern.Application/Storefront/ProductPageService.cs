using TimeoutPattern.Application.Abstractions;
using TimeoutPattern.Core.Execution;

namespace TimeoutPattern.Application.Storefront;

/// <summary>
/// Composes a product page from three dependencies with different criticality:
/// catalog and pricing are required (their timeouts fail the request),
/// inventory is optional (its timeout degrades the page to "stock unknown").
/// </summary>
public sealed class ProductPageService(
    ICatalog catalog,
    PricingService pricing,
    IInventoryGateway inventory,
    ITimeoutExecutor executor)
{
    public async Task<ProductPage?> FindAsync(string sku, CancellationToken cancellationToken)
    {
        var product = await executor.ExecuteAsync(
            Operations.CatalogRead,
            token => catalog.FindAsync(sku, token),
            cancellationToken);

        if (product is null)
        {
            return null;
        }

        var degradations = new List<Degradation>();
        var priceTask = executor.ExecuteAsync(
            Operations.PricingQuote,
            token => pricing.QuoteAsync(product, token),
            cancellationToken);
        var stockTask = ReadStockOrDegradeAsync(sku, degradations, cancellationToken);

        await Task.WhenAll(priceTask, stockTask);

        return new ProductPage(product, await priceTask, await stockTask, degradations);
    }

    private async Task<StockLevel> ReadStockOrDegradeAsync(
        string sku,
        List<Degradation> degradations,
        CancellationToken cancellationToken)
    {
        try
        {
            return await executor.ExecuteAsync(
                Operations.InventoryLookup,
                token => inventory.GetStockAsync(sku, token),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Only one task touches the list, so no lock is needed.
            degradations.Add(new Degradation(Dependencies.Inventory, ex.Message));
            return StockLevel.Unknown;
        }
    }
}
