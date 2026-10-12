using System.Globalization;
using TimeoutPattern.Application.Abstractions;
using TimeoutPattern.Core.Errors;
using TimeoutPattern.Core.Execution;
using TimeoutPattern.Core.Forecasting;

namespace TimeoutPattern.Application.Reports;

/// <summary>
/// A budget-aware loop. It learns how long a day takes, and stops while it can still
/// answer: partial data delivered on time beats a complete answer nobody receives.
/// </summary>
public sealed class SalesReportService(
    ISalesLedger ledger,
    ITimeoutExecutor executor,
    ITimeoutPlanner planner,
    TimeProvider clock)
{
    public async Task<SalesReport> GenerateAsync(int days, CancellationToken cancellationToken)
    {
        var forecast = new EwmaDurationEstimator();
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var completed = 0;
        var orders = 0L;
        var revenue = 0m;
        string? stopReason = null;

        while (completed < days)
        {
            if (forecast.Samples > 0 && WouldOverrun(forecast) is { } reason)
            {
                stopReason = reason;
                break;
            }

            var day = today.AddDays(-completed);
            var startedAt = clock.GetTimestamp();
            try
            {
                var sales = await executor.ExecuteAsync(
                    Operations.ReportsDay,
                    token => ledger.GetDailySalesAsync(day, token),
                    cancellationToken);

                orders += sales.Orders;
                revenue += sales.Revenue;
                completed++;
            }
            catch (DeadlineException ex)
            {
                stopReason = ex.Message;
                break;
            }

            forecast.Observe(clock.GetElapsedTime(startedAt));
        }

        return new SalesReport(
            days,
            completed,
            orders,
            revenue,
            IsPartial: completed < days,
            stopReason,
            Math.Round(forecast.Mean.TotalMilliseconds, 1));
    }

    private string? WouldOverrun(EwmaDurationEstimator forecast)
    {
        // The plan already subtracts the reserve kept for serializing the response.
        var available = planner.Plan(Operations.ReportsDay).Effective;
        var needed = forecast.UpperBound();

        return needed <= available
            ? null
            : string.Create(
                CultureInfo.InvariantCulture,
                $"Stopped early: the next day is forecast at {needed.TotalMilliseconds:F0} ms (mean + 2σ), but only {available.TotalMilliseconds:F0} ms can be spent.");
    }
}
