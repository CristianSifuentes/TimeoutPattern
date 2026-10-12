using TimeoutPattern.Application.Abstractions;

namespace TimeoutPattern.Infrastructure.Simulation;

/// <summary>
/// Stands in for a synchronous vendor SDK with no cancellation support. It ignores the
/// token on purpose. A cooperative timeout cannot cut it short; the <c>Pessimistic</c>
/// strategy configured for <c>pricing.tax</c> stops waiting for it.
/// </summary>
internal sealed class LegacyTaxCalculator(ILatencySimulator latency) : ITaxCalculator
{
    private const decimal Rate = 0.16m;

    public Task<decimal> ComputeAsync(decimal amount, CancellationToken cancellationToken)
    {
        var delay = latency.LatencyFor(Dependencies.Tax);

        return Task.Run(() =>
        {
            Thread.Sleep(delay);
            return decimal.Round(amount * Rate, 2);
        }, CancellationToken.None);
    }
}
