namespace TimeoutPattern.Core.Budgets;

/// <summary>The innermost budget of the current async flow, or <c>null</c> outside any.</summary>
public interface ITimeoutBudgetAccessor
{
    TimeoutBudget? Current { get; set; }
}
