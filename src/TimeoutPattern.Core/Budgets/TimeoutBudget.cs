using System.Globalization;

namespace TimeoutPattern.Core.Budgets;

/// <summary>
/// A span of time that started at a known instant. Budgets nest: a request budget
/// contains operation budgets, which contain further operation budgets.
/// A child can never outlive its parent.
/// </summary>
public sealed class TimeoutBudget
{
    private readonly TimeProvider _clock;
    private readonly long _startedAt;

    private TimeoutBudget(string name, TimeoutScope scope, TimeSpan timeout, TimeProvider clock, TimeoutBudget? parent)
    {
        Name = name;
        Scope = scope;
        Timeout = timeout;
        Parent = parent;
        _clock = clock;
        _startedAt = clock.GetTimestamp();
    }

    public string Name { get; }

    public TimeoutScope Scope { get; }

    public TimeSpan Timeout { get; }

    public TimeoutBudget? Parent { get; }

    public TimeSpan Elapsed => _clock.GetElapsedTime(_startedAt);

    public TimeSpan Remaining
    {
        get
        {
            var own = Timeout - Elapsed;
            var remaining = Parent is null ? own : Min(own, Parent.Remaining);
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public bool IsExhausted => Remaining == TimeSpan.Zero;

    /// <summary>Readable lineage for logs and diagnostics, e.g. <c>products.read › pricing.quote › pricing.tax</c>.</summary>
    public string Path => Parent is null ? Name : $"{Parent.Path} › {Name}";

    public static TimeoutBudget ForRequest(string name, TimeSpan timeout, TimeProvider clock) =>
        new(name, TimeoutScope.Request, timeout, clock, parent: null);

    public static TimeoutBudget ForOperation(string name, TimeSpan timeout, TimeProvider clock, TimeoutBudget? parent) =>
        new(name, TimeoutScope.Operation, timeout, clock, parent);

    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{Path} ({Remaining.TotalMilliseconds:F0} of {Timeout.TotalMilliseconds:F0} ms left)");

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a <= b ? a : b;
}
