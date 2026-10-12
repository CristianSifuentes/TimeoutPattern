using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using TimeoutPattern.Application.Abstractions;
using TimeoutPattern.Application.Storefront;

namespace TimeoutPattern.Infrastructure.Http;

internal sealed class InventoryHttpGateway(HttpClient http, ILatencySimulator latency) : IInventoryGateway
{
    public async Task<StockLevel> GetStockAsync(string sku, CancellationToken cancellationToken)
    {
        var simulatedMs = (long)latency.LatencyFor(Dependencies.Inventory).TotalMilliseconds;
        var uri = string.Create(
            CultureInfo.InvariantCulture,
            $"api/warehouse/stock/{Uri.EscapeDataString(sku)}?latencyMs={simulatedMs}");

        using var response = await http.GetAsync(uri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.GatewayTimeout)
        {
            throw new TimeoutException("The warehouse used up the deadline it was given and returned 504.");
        }

        response.EnsureSuccessStatusCode();

        var stock = await response.Content.ReadFromJsonAsync<WarehouseStock>(cancellationToken)
            ?? throw new InvalidOperationException("The warehouse returned an empty body.");

        return new StockLevel(stock.Available, stock.Available > 0 ? "in-stock" : "sold-out");
    }

    private sealed record WarehouseStock(string Sku, int Available);
}
