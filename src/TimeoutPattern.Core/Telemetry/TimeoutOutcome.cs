namespace TimeoutPattern.Core.Telemetry;

public enum TimeoutOutcome
{
    Succeeded,
    TimedOut,
    Rejected,
    Cancelled,
    Faulted,
}
