namespace TimeoutPattern.Core.Policies;

/// <param name="Operation">The name the operation was invoked with.</param>
/// <param name="MatchedKey">Most specific configured key in the lineage, or <c>null</c> if only defaults applied.</param>
public sealed record OperationPolicy(
    string Operation,
    string? MatchedKey,
    TimeSpan Timeout,
    TimeSpan Reserve,
    TimeoutStrategy Strategy);
