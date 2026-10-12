using System.Globalization;
using TimeoutPattern.Core.Budgets;
using TimeoutPattern.Core.Policies;

namespace TimeoutPattern.Api.Timeouts;

/// <summary>
/// Shows the deadline on every response. <c>Server-Timing</c> appears in the browser's
/// DevTools timing panel, so the budget can be checked without reading logs.
/// </summary>
internal static class TimeoutResponseHeaders
{
    public static void Apply(HttpResponse response, RequestTimeoutDecision decision, TimeoutBudget budget)
    {
        var source = decision.Source.ToString().ToLowerInvariant();

        response.Headers.Append("Server-Timing", string.Create(
            CultureInfo.InvariantCulture,
            $"deadline;desc=\"{decision.PolicyName} ({source})\";dur={decision.Timeout.TotalMilliseconds:F0}, budget-spent;dur={budget.Elapsed.TotalMilliseconds:F1}"));
        response.Headers["X-Timeout-Policy"] = decision.PolicyName;
        response.Headers["X-Timeout-Remaining-Ms"] = DeadlineHeader.Format(budget.Remaining);
    }
}
