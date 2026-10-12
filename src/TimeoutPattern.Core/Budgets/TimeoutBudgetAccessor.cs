namespace TimeoutPattern.Core.Budgets;

/// <summary>
/// Flows the budget with the <see cref="ExecutionContext"/>, the same way <c>IHttpContextAccessor</c> flows the request.
/// A value set inside an async method is visible to everything it awaits, and reverts for its caller when it returns.
/// </summary>
public sealed class TimeoutBudgetAccessor : ITimeoutBudgetAccessor
{
    private static readonly AsyncLocal<TimeoutBudget?> s_current = new();

    public TimeoutBudget? Current
    {
        get => s_current.Value;
        set => s_current.Value = value;
    }
}
