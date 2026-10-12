using TimeoutPattern.Application.Reports;
using TimeoutPattern.Application.Storefront;

namespace TimeoutPattern.Application.Abstractions;

public interface ICatalog
{
    Task<Product?> FindAsync(string sku, CancellationToken cancellationToken);
}

public interface IDemandModel
{
    /// <summary>Expected price multiplier given simulated demand; CPU-bound.</summary>
    Task<double> EstimateMultiplierAsync(Product product, CancellationToken cancellationToken);
}

public interface ITaxCalculator
{
    Task<decimal> ComputeAsync(decimal amount, CancellationToken cancellationToken);
}

public interface IInventoryGateway
{
    Task<StockLevel> GetStockAsync(string sku, CancellationToken cancellationToken);
}

public interface ISalesLedger
{
    Task<DailySales> GetDailySalesAsync(DateOnly day, CancellationToken cancellationToken);
}
