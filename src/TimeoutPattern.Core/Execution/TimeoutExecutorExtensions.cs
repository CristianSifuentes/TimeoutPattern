namespace TimeoutPattern.Core.Execution;

public static class TimeoutExecutorExtensions
{
    public static Task ExecuteAsync(
        this ITimeoutExecutor executor,
        string operation,
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default) =>
        executor.ExecuteAsync(
            operation,
            async token =>
            {
                await work(token).ConfigureAwait(false);
                return true;
            },
            cancellationToken);
}
