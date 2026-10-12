namespace TimeoutPattern.Core.Errors;

/// <summary>
/// Base for every timeout this library raises. Derives from <see cref="TimeoutException"/>,
/// never from <see cref="OperationCanceledException"/>: running out of time is a failure,
/// while cancellation is the caller changing its mind.
/// </summary>
public abstract class DeadlineException(string message, TimeoutDiagnostic diagnostic, Exception? innerException)
    : TimeoutException(message, innerException)
{
    public TimeoutDiagnostic Diagnostic { get; } = diagnostic;
}
