using TimeoutPattern.Core.Policies;

namespace TimeoutPattern.Core.Configuration;

/// <summary>
/// Root of the <c>Timeouts</c> configuration section.
/// Read through <c>IOptionsMonitor</c> on every resolution, so edits apply without a restart.
/// </summary>
public sealed class TimeoutOptions
{
    public const string SectionName = "Timeouts";

    /// <summary>Deadline for a request whose endpoint names no policy, or names one that is not configured.</summary>
    public TimeSpan DefaultRequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Timeout for an operation with no matching entry anywhere in its key lineage.</summary>
    public TimeSpan DefaultOperationTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Below this much effective time, an operation is rejected without being started.
    /// Starting work that cannot finish only burns a connection and a thread.
    /// </summary>
    public TimeSpan MinimumViableBudget { get; set; } = TimeSpan.FromMilliseconds(20);

    public DeadlinePropagationOptions Propagation { get; set; } = new();

    /// <summary>Request policies keyed by dotted name. <c>products.read</c> inherits unset fields from <c>products</c>.</summary>
    public Dictionary<string, RequestPolicyOptions> Requests { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Operation policies keyed by dotted name. <c>pricing.tax</c> inherits unset fields from <c>pricing</c>.</summary>
    public Dictionary<string, OperationPolicyOptions> Operations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class DeadlinePropagationOptions
{
    /// <summary>Header carrying the remaining budget in whole milliseconds, inbound and outbound.</summary>
    public string HeaderName { get; set; } = "X-Request-Timeout-Ms";

    /// <summary>Whether an inbound header may shorten a request's deadline. It can never lengthen it.</summary>
    public bool HonorInbound { get; set; } = true;

    /// <summary>Whether outgoing HTTP calls advertise the caller's remaining budget downstream.</summary>
    public bool PropagateOutbound { get; set; } = true;

    /// <summary>Subtracted from the propagated budget so the downstream gives up before the caller does.</summary>
    public TimeSpan NetworkAllowance { get; set; } = TimeSpan.FromMilliseconds(15);
}

public sealed class RequestPolicyOptions
{
    public TimeSpan? Timeout { get; set; }

    public bool? HonorInboundDeadline { get; set; }
}

public sealed class OperationPolicyOptions
{
    public TimeSpan? Timeout { get; set; }

    /// <summary>Time held back from the enclosing budget for the work that follows this operation (e.g. serializing the response).</summary>
    public TimeSpan? Reserve { get; set; }

    public TimeoutStrategy? Strategy { get; set; }
}
