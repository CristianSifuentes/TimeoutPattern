namespace TimeoutPattern.Core.Execution;

public interface ITimeoutExecutor
{
    /// <summary>
    /// Runs <paramref name="work"/> under the timeout planned for <paramref name="operation"/>.
    /// </summary>
    /// <exception cref="Errors.OperationTimeoutException">The planned timeout elapsed first.</exception>
    /// <exception cref="Errors.BudgetExhaustedException">Too little budget remained to start.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled by the caller.</exception>
    Task<T> ExecuteAsync<T>(string operation, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default);
}
