namespace TimeoutPattern.Application;

/// <summary>
/// Operation names, which double as configuration keys. Dots form a hierarchy:
/// <c>catalog.read</c> takes any field it leaves unset from <c>catalog</c>.
/// </summary>
public static class Operations
{
    public const string CatalogRead = "catalog.read";
    public const string PricingQuote = "pricing.quote";
    public const string PricingTax = "pricing.tax";
    public const string InventoryLookup = "inventory.lookup";
    public const string ReportsDay = "reports.day";
}
