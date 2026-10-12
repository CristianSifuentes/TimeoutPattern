using Microsoft.Extensions.Options;
using TimeoutPattern.Core.Configuration;

namespace TimeoutPattern.Core.Policies;

/// <summary>
/// Resolves policies by walking a dotted key from most to least specific.
/// Each field inherits on its own, so <c>pricing.tax</c> can change only the strategy
/// and still take its timeout from <c>pricing</c>.
/// </summary>
public sealed class TimeoutPolicyResolver(IOptionsMonitor<TimeoutOptions> options) : ITimeoutPolicyResolver
{
    public const string DefaultPolicyName = "default";

    public OperationPolicy ResolveOperation(string operation)
    {
        var current = options.CurrentValue;
        string? matchedKey = null;
        TimeSpan? timeout = null;
        TimeSpan? reserve = null;
        TimeoutStrategy? strategy = null;

        foreach (var key in PolicyKey.Lineage(operation))
        {
            if (!current.Operations.TryGetValue(key, out var node))
            {
                continue;
            }

            matchedKey ??= key;
            timeout ??= node.Timeout;
            reserve ??= node.Reserve;
            strategy ??= node.Strategy;
        }

        return new OperationPolicy(
            operation,
            matchedKey,
            timeout ?? current.DefaultOperationTimeout,
            reserve ?? TimeSpan.Zero,
            strategy ?? TimeoutStrategy.Cooperative);
    }

    public RequestTimeoutDecision ResolveRequest(string? policyName, TimeSpan? inboundDeadline)
    {
        var current = options.CurrentValue;
        string? matchedKey = null;
        TimeSpan? timeout = null;
        bool? honorInbound = null;

        foreach (var key in policyName is null ? Enumerable.Empty<string>() : PolicyKey.Lineage(policyName))
        {
            if (!current.Requests.TryGetValue(key, out var node))
            {
                continue;
            }

            matchedKey ??= key;
            timeout ??= node.Timeout;
            honorInbound ??= node.HonorInboundDeadline;
        }

        var name = policyName ?? DefaultPolicyName;
        var policyTimeout = timeout ?? current.DefaultRequestTimeout;
        var source = matchedKey is null ? DeadlineSource.Default : DeadlineSource.Policy;

        // An inbound deadline may only tighten: a caller cannot buy itself more server time than policy allows.
        if ((honorInbound ?? current.Propagation.HonorInbound) && inboundDeadline is { } inbound && inbound < policyTimeout)
        {
            return new RequestTimeoutDecision(name, inbound, DeadlineSource.Inbound, policyTimeout);
        }

        return new RequestTimeoutDecision(name, policyTimeout, source, policyTimeout);
    }
}
