using Microsoft.Extensions.Options;
using TimeoutPattern.Core.Budgets;
using TimeoutPattern.Core.Configuration;
using TimeoutPattern.Core.Policies;

namespace TimeoutPattern.Core.Execution;

/// <summary>
/// effective = min(policy timeout, enclosing remaining − reserve).
/// The policy says how long the operation deserves; the budget says how long the caller can still wait.
/// </summary>
public sealed class TimeoutPlanner(
    ITimeoutPolicyResolver resolver,
    ITimeoutBudgetAccessor budgets,
    IOptionsMonitor<TimeoutOptions> options) : ITimeoutPlanner
{
    public TimeoutPlan Plan(string operation)
    {
        var policy = resolver.ResolveOperation(operation);
        var enclosing = budgets.Current;
        var effective = policy.Timeout;
        var boundBy = TimeoutBound.OperationPolicy;

        if (enclosing is not null)
        {
            var available = enclosing.Remaining - policy.Reserve;
            if (available < effective)
            {
                effective = available > TimeSpan.Zero ? available : TimeSpan.Zero;
                boundBy = TimeoutBound.EnclosingBudget;
            }
        }

        return new TimeoutPlan(policy, effective, boundBy, enclosing, options.CurrentValue.MinimumViableBudget);
    }
}
