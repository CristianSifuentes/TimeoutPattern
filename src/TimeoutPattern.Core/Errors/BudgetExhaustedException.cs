using System.Globalization;
using TimeoutPattern.Core.Execution;

namespace TimeoutPattern.Core.Errors;

/// <summary>Raised before an operation starts, when the time left could not plausibly be enough.</summary>
public sealed class BudgetExhaustedException(TimeoutPlan plan)
    : DeadlineException(Describe(plan), TimeoutDiagnostic.ForOperation(plan, TimeSpan.Zero), innerException: null)
{
    public TimeoutPlan Plan { get; } = plan;

    private static string Describe(TimeoutPlan plan) => string.Create(
        CultureInfo.InvariantCulture,
        $"Operation '{plan.Operation}' was not started: {plan.Effective.TotalMilliseconds:F0} ms remained, below the {plan.MinimumViable.TotalMilliseconds:F0} ms minimum.");
}
