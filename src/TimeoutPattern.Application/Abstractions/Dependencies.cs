namespace TimeoutPattern.Application.Abstractions;

/// <summary>Names of the simulated downstream dependencies, used to look up injected latency.</summary>
public static class Dependencies
{
    public const string Catalog = "catalog";
    public const string Demand = "demand";
    public const string Tax = "tax";
    public const string Inventory = "inventory";
    public const string Ledger = "ledger";
}
