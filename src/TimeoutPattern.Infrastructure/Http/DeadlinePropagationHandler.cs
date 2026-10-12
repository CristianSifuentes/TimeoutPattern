using Microsoft.Extensions.Options;
using TimeoutPattern.Core.Budgets;
using TimeoutPattern.Core.Configuration;

namespace TimeoutPattern.Infrastructure.Http;

/// <summary>
/// Tells the downstream service how long this caller will still wait, as gRPC does with deadlines.
/// The downstream service can then give up as soon as its answer would arrive too late to be used,
/// instead of finishing work for a caller that has already left.
/// </summary>
public sealed class DeadlinePropagationHandler(
    ITimeoutBudgetAccessor budgets,
    IOptionsMonitor<TimeoutOptions> options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var propagation = options.CurrentValue.Propagation;

        if (propagation.PropagateOutbound && budgets.Current is { } budget)
        {
            request.Headers.Remove(propagation.HeaderName);
            request.Headers.TryAddWithoutValidation(
                propagation.HeaderName,
                DeadlineHeader.Format(budget.Remaining - propagation.NetworkAllowance));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
