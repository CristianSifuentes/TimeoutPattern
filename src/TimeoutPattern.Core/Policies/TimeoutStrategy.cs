namespace TimeoutPattern.Core.Policies;

public enum TimeoutStrategy
{
    /// <summary>
    /// Signal the token and wait for the work to notice it. Nothing is left running,
    /// but work that ignores its token will overrun the timeout.
    /// </summary>
    Cooperative,

    /// <summary>
    /// Signal the token and stop waiting at once. The caller is released on time,
    /// but non-cooperative work keeps running, unobserved, until it finishes on its own.
    /// Reserve for dependencies you cannot make cancellable.
    /// </summary>
    Pessimistic,
}
