using Microsoft.Extensions.Logging;
using TimeoutPattern.Core.Execution;

namespace TimeoutPattern.Core.Telemetry;

public static partial class TimeoutLog
{
    [LoggerMessage(1001, LogLevel.Warning,
        "Timeout: {Path} gave up after {ElapsedMs:F0} ms (allowed {EffectiveMs:F0} ms, bound by {BoundBy})")]
    public static partial void OperationTimedOut(ILogger logger, string path, double elapsedMs, double effectiveMs, TimeoutBound boundBy);

    [LoggerMessage(1002, LogLevel.Warning,
        "Rejected: {Path} had {EffectiveMs:F0} ms left, below the {MinimumMs:F0} ms minimum; not started")]
    public static partial void BudgetExhausted(ILogger logger, string path, double effectiveMs, double minimumMs);

    [LoggerMessage(1003, LogLevel.Warning,
        "Abandoned: {Path} ignored its cancellation token and is still running in the background")]
    public static partial void WorkAbandoned(ILogger logger, string path);

    [LoggerMessage(1004, LogLevel.Information,
        "Deadline: request {Policy} exceeded {TimeoutMs:F0} ms ({Source})")]
    public static partial void RequestDeadlineExceeded(ILogger logger, string policy, double timeoutMs, string source);

    [LoggerMessage(1005, LogLevel.Information,
        "Client went away: request {Policy} aborted after {ElapsedMs:F0} ms")]
    public static partial void ClientAborted(ILogger logger, string policy, double elapsedMs);
}
