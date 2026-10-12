using TimeoutPattern.Core.Budgets;
using TimeoutPattern.Core.Policies;

namespace TimeoutPattern.Core.Execution;

public enum TimeoutBound
{
    /// <summary>The operation's own policy was the tighter limit.</summary>
    OperationPolicy,

    /// <summary>What was left of the enclosing budget, minus the reserve, was the tighter limit.</summary>
    EnclosingBudget,
}

/// <summary>What an operation is about to be allowed, and why.</summary>
public sealed record TimeoutPlan(
    OperationPolicy Policy,
    TimeSpan Effective,
    TimeoutBound BoundBy,
    TimeoutBudget? Enclosing,
    TimeSpan MinimumViable)
{
    public string Operation => Policy.Operation;

    public bool IsViable => Effective >= MinimumViable && Effective > TimeSpan.Zero;

    public string Path => Enclosing is null ? Operation : $"{Enclosing.Path} › {Operation}";
}
