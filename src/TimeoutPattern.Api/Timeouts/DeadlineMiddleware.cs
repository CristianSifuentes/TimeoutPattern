using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Options;
using TimeoutPattern.Core.Budgets;
using TimeoutPattern.Core.Configuration;
using TimeoutPattern.Core.Errors;
using TimeoutPattern.Core.Policies;
using TimeoutPattern.Core.Telemetry;

namespace TimeoutPattern.Api.Timeouts;

/// <summary>
/// Gives each request a deadline and a root budget. The deadline is the endpoint's policy,
/// shortened by the caller's propagated deadline if that is sooner.
/// <see cref="HttpContext.RequestAborted"/> is replaced with a token that fires on
/// client disconnect or deadline, whichever happens first, so any code that already
/// flows the request token respects the deadline with no changes.
/// Timeouts are answered here as 504s instead of being rethrown, because they are an
/// expected outcome, and the exception-handler middleware would log each one as an error.
/// </summary>
internal sealed class DeadlineMiddleware(
    RequestDelegate next,
    ITimeoutPolicyResolver resolver,
    ITimeoutBudgetAccessor budgets,
    IOptionsMonitor<TimeoutOptions> options,
    TimeoutTelemetry telemetry,
    DeadlineProblemWriter problems,
    TimeProvider clock,
    ILogger<DeadlineMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<UnboundedRequestAttribute>() is not null)
        {
            await next(context);
            return;
        }

        var decision = resolver.ResolveRequest(
            endpoint?.Metadata.GetMetadata<TimeoutPolicyAttribute>()?.Name,
            ReadInboundDeadline(context.Request));

        var budget = TimeoutBudget.ForRequest(decision.PolicyName, decision.Timeout, clock);
        using var deadline = new CancellationTokenSource(decision.Timeout, clock);
        using var requestScope = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, deadline.Token);

        var clientAborted = context.RequestAborted;
        context.RequestAborted = requestScope.Token;
        budgets.Current = budget;
        context.Response.OnStarting(() =>
        {
            TimeoutResponseHeaders.Apply(context.Response, decision, budget);
            return Task.CompletedTask;
        });

        var outcome = "faulted";
        DeadlineException? timeout = null;
        try
        {
            await next(context);
            outcome = "completed";
        }
        catch (OperationCanceledException ex) when (deadline.IsCancellationRequested && !clientAborted.IsCancellationRequested)
        {
            outcome = "deadline_exceeded";
            timeout = new RequestDeadlineExceededException(decision, budget.Elapsed, ex);
            TimeoutLog.RequestDeadlineExceeded(logger, decision.PolicyName, decision.Timeout.TotalMilliseconds, decision.Source.ToString());
        }
        catch (OperationCanceledException) when (clientAborted.IsCancellationRequested)
        {
            // Nobody is listening for the answer; there is nothing useful to write.
            outcome = "client_aborted";
            TimeoutLog.ClientAborted(logger, decision.PolicyName, budget.Elapsed.TotalMilliseconds);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }
        }
        catch (DeadlineException ex)
        {
            outcome = "dependency_timeout";
            timeout = ex;
        }
        finally
        {
            telemetry.RecordRequest(decision, outcome);
            context.RequestAborted = clientAborted;
            budgets.Current = null;
        }

        // Written after the original token is restored: the deadline token has already fired.
        if (timeout is not null && !await problems.TryWriteAsync(context, timeout))
        {
            ExceptionDispatchInfo.Throw(timeout);
        }
    }

    private TimeSpan? ReadInboundDeadline(HttpRequest request) =>
        DeadlineHeader.TryParse(request.Headers[options.CurrentValue.Propagation.HeaderName], out var deadline)
            ? deadline
            : null;
}
