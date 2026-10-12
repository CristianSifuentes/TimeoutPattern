using TimeoutPattern.Application.Abstractions;
using TimeoutPattern.Application.Storefront;

namespace TimeoutPattern.Infrastructure.Simulation;

/// <summary>
/// Estimates a demand multiplier by sampling a log-normal demand curve.
/// It is CPU-bound, so it polls the token on a stride: often enough to stop within
/// a millisecond or two, rarely enough that the check costs nothing measurable.
/// </summary>
internal sealed class MonteCarloDemandModel(ILatencySimulator latency, TimeProvider clock) : IDemandModel
{
    private const int SampleCount = 200_000;
    private const int CancellationStride = 4_096;
    private const double Volatility = 0.15;

    public async Task<double> EstimateMultiplierAsync(Product product, CancellationToken cancellationToken)
    {
        await Task.Delay(latency.LatencyFor(Dependencies.Demand), clock, cancellationToken);
        return Simulate(StableSeed(product.Sku), cancellationToken);
    }

    private static double Simulate(int seed, CancellationToken cancellationToken)
    {
        var random = new Random(seed);
        var sum = 0.0;

        for (var i = 0; i < SampleCount; i++)
        {
            if (i % CancellationStride == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Box–Muller: two uniforms → one standard normal → log-normal demand.
            var u1 = 1.0 - random.NextDouble();
            var u2 = random.NextDouble();
            var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

            sum += Math.Clamp(Math.Exp(Volatility * z), 0.85, 1.25);
        }

        return Math.Round(sum / SampleCount, 4);
    }

    /// <summary>FNV-1a, because <see cref="string.GetHashCode()"/> changes from one process to the next.</summary>
    private static int StableSeed(string value)
    {
        var hash = 2166136261u;
        foreach (var c in value)
        {
            hash = (hash ^ c) * 16777619u;
        }

        return unchecked((int)hash);
    }
}
