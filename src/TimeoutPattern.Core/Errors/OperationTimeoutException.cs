using System.Globalization;
using TimeoutPattern.Core.Execution;

namespace TimeoutPattern.Core.Errors;

public sealed class OperationTimeoutException(TimeoutPlan plan, TimeSpan elapsed, Exception? innerException)
    : DeadlineException(Describe(plan, elapsed), TimeoutDiagnostic.ForOperation(plan, elapsed), innerException)
{
    public TimeoutPlan Plan { get; } = plan;

    public TimeSpan Elapsed { get; } = elapsed;

    private static string Describe(TimeoutPlan plan, TimeSpan elapsed)
    {
        var reason = plan.BoundBy == TimeoutBound.OperationPolicy
            ? "its own policy"
            : $"what was left of '{plan.Enclosing?.Name}'";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Operation '{plan.Operation}' gave up after {elapsed.TotalMilliseconds:F0} ms; it was allowed {plan.Effective.TotalMilliseconds:F0} ms by {reason}.");
    }
}
