using Microsoft.AspNetCore.Mvc;
using TimeoutPattern.Api.Timeouts;

namespace TimeoutPattern.Api.Controllers;

/// <summary>
/// Simulated downstream service, hosted in the same process for the demo. It is reached
/// over real HTTP, so it receives the propagated deadline exactly as a separate service would.
/// </summary>
[ApiController]
[Route("api/warehouse")]
[TimeoutPolicy("warehouse.stock")]
public sealed class WarehouseController(TimeProvider clock) : ControllerBase
{
    private const int MaxLatencyMs = 30_000;

    [HttpGet("stock/{sku}")]
    public async Task<StockResponse> Stock(string sku, [FromQuery] int latencyMs, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(Math.Clamp(latencyMs, 0, MaxLatencyMs)), clock, cancellationToken);

        var available = Math.Abs(sku.ToUpperInvariant().Sum(c => c)) % 40;
        return new StockResponse(sku, available);
    }

    public sealed record StockResponse(string Sku, int Available);
}
