using TimeoutPattern.Application.Abstractions;
using TimeoutPattern.Core.Execution;

namespace TimeoutPattern.Application.Storefront;

public sealed class PricingService(IDemandModel demand, ITaxCalculator tax, ITimeoutExecutor executor)
{
    private const string Currency = "USD";

    /// <summary>
    /// Runs inside <c>pricing.quote</c>, so the tax call below gets a slice of
    /// that budget rather than of the whole request: budgets nest.
    /// </summary>
    public async Task<PriceQuote> QuoteAsync(Product product, CancellationToken cancellationToken)
    {
        var multiplier = await demand.EstimateMultiplierAsync(product, cancellationToken);
        var subtotal = decimal.Round(product.BasePrice * (decimal)multiplier, 2);

        var taxAmount = await executor.ExecuteAsync(
            Operations.PricingTax,
            token => tax.ComputeAsync(subtotal, token),
            cancellationToken);

        return new PriceQuote(subtotal, taxAmount, subtotal + taxAmount, Currency, multiplier);
    }
}
