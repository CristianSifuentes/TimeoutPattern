namespace TimeoutPattern.Infrastructure.Http;

public sealed class WarehouseOptions
{
    public const string SectionName = "Warehouse";

    /// <summary>Name of the typed <see cref="HttpClient"/>, so hosts and tests can reconfigure it.</summary>
    public const string HttpClientName = "warehouse";

    public Uri? BaseUrl { get; set; }
}
