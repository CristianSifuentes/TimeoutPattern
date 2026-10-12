namespace TimeoutPattern.Core.Policies;

public enum DeadlineSource
{
    /// <summary>No request policy matched; the global default applied.</summary>
    Default,

    /// <summary>A configured request policy set the deadline.</summary>
    Policy,

    /// <summary>The caller advertised a shorter deadline, and it was honored.</summary>
    Inbound,
}

/// <param name="PolicyTimeout">What the server would have allowed on its own, before any inbound deadline.</param>
public sealed record RequestTimeoutDecision(
    string PolicyName,
    TimeSpan Timeout,
    DeadlineSource Source,
    TimeSpan PolicyTimeout);
