namespace TimeoutPattern.Application.Storefront;

public sealed record Product(string Sku, string Name, string Category, decimal BasePrice);

public sealed record PriceQuote(decimal Subtotal, decimal Tax, decimal Total, string Currency, double DemandMultiplier);

public sealed record StockLevel(int? Available, string Status)
{
    public static StockLevel Unknown { get; } = new(null, "unknown");
}

/// <summary>A dependency that was skipped or failed, and what the page shows instead.</summary>
public sealed record Degradation(string Dependency, string Reason);

public sealed record ProductPage(Product Product, PriceQuote Price, StockLevel Stock, IReadOnlyList<Degradation> Degradations);
