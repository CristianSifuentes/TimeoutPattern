using TimeoutPattern.Application.Abstractions;
using TimeoutPattern.Application.Reports;

namespace TimeoutPattern.Infrastructure.Simulation;

internal sealed class SimulatedSalesLedger(ILatencySimulator latency, TimeProvider clock) : ISalesLedger
{
    public async Task<DailySales> GetDailySalesAsync(DateOnly day, CancellationToken cancellationToken)
    {
        await Task.Delay(latency.LatencyFor(Dependencies.Ledger), clock, cancellationToken);

        var random = new Random(day.DayNumber);
        var orders = random.Next(40, 160);
        var revenue = decimal.Round(orders * (decimal)(35 + random.NextDouble() * 60), 2);

        return new DailySales(day, orders, revenue);
    }
}
