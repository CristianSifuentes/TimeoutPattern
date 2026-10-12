namespace TimeoutPattern.Core.Execution;

public interface ITimeoutPlanner
{
    /// <summary>Computes the timeout <paramref name="operation"/> would get if it started now, in the current budget.</summary>
    TimeoutPlan Plan(string operation);
}
