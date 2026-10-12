using System.Collections.Frozen;
using TimeoutPattern.Application.Abstractions;
using TimeoutPattern.Application.Storefront;

namespace TimeoutPattern.Infrastructure.Simulation;

/// <summary>A studio supply shop for people who are equal parts artist and scientist.</summary>
internal sealed class InMemoryCatalog(ILatencySimulator latency, TimeProvider clock) : ICatalog
{
    private static readonly FrozenDictionary<string, Product> s_products = new Product[]
    {
        new("TEL-080", "Refractor Telescope 80 mm", "Optics", 349.00m),
        new("SPC-001", "Pocket Diffraction Spectroscope", "Optics", 89.90m),
        new("PIG-LAP", "Lapis Lazuli Pigment, 50 g", "Pigments", 64.50m),
        new("CAN-PHI", "Golden-Ratio Canvas 61.8 × 100 cm", "Canvas", 42.00m),
        new("LAB-GRD", "Dot-Grid Lab Notebook", "Paper", 12.75m),
    }.ToFrozenDictionary(product => product.Sku, StringComparer.OrdinalIgnoreCase);

    public async Task<Product?> FindAsync(string sku, CancellationToken cancellationToken)
    {
        await Task.Delay(latency.LatencyFor(Dependencies.Catalog), clock, cancellationToken);
        return s_products.GetValueOrDefault(sku);
    }
}
