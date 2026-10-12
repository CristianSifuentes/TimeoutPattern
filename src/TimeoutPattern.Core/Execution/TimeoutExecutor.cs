using TimeoutPattern.Core.Budgets;
using TimeoutPattern.Core.Errors;
using TimeoutPattern.Core.Policies;
using TimeoutPattern.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace TimeoutPattern.Core.Execution;

public sealed class TimeoutExecutor(
    ITimeoutPlanner planner,
    ITimeoutBudgetAccessor budgets,
    TimeProvider clock,
    TimeoutTelemetry telemetry,
    ILogger<TimeoutExecutor> logger) : ITimeoutExecutor
{
    public async Task<T> ExecuteAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(work);
        cancellationToken.ThrowIfCancellationRequested();

        var plan = planner.Plan(operation);
        if (!plan.IsViable)
        {
            telemetry.RecordOperation(plan, TimeoutOutcome.Rejected, TimeSpan.Zero);
            TimeoutLog.BudgetExhausted(logger, plan.Path, plan.Effective.TotalMilliseconds, plan.MinimumViable.TotalMilliseconds);
            throw new BudgetExhaustedException(plan);
        }

        using var timeoutSource = new CancellationTokenSource(plan.Effective, clock);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        // Everything the work awaits sees this operation as the innermost budget,
        // so nested operations and outgoing HTTP calls are bounded by it.
        var enclosing = budgets.Current;
        budgets.Current = TimeoutBudget.ForOperation(operation, plan.Effective, clock, enclosing);

        var startedAt = clock.GetTimestamp();
        Task<T>? running = null;
        try
        {
            running = work(linkedSource.Token);
            var result = await Supervise(running, plan.Policy.Strategy, linkedSource.Token).ConfigureAwait(false);

            telemetry.RecordOperation(plan, TimeoutOutcome.Succeeded, clock.GetElapsedTime(startedAt));
            return result;
        }
        catch (OperationCanceledException ex) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            var elapsed = clock.GetElapsedTime(startedAt);
            telemetry.RecordOperation(plan, TimeoutOutcome.TimedOut, elapsed);
            TimeoutLog.OperationTimedOut(logger, plan.Path, elapsed.TotalMilliseconds, plan.Effective.TotalMilliseconds, plan.BoundBy);

            if (running is { IsCompleted: false })
            {
                TimeoutLog.WorkAbandoned(logger, plan.Path);
            }

            throw new OperationTimeoutException(plan, elapsed, ex);
        }
        catch (OperationCanceledException)
        {
            telemetry.RecordOperation(plan, TimeoutOutcome.Cancelled, clock.GetElapsedTime(startedAt));
            throw;
        }
        catch (Exception)
        {
            telemetry.RecordOperation(plan, TimeoutOutcome.Faulted, clock.GetElapsedTime(startedAt));
            throw;
        }
        finally
        {
            budgets.Current = enclosing;
        }
    }

    private static Task<T> Supervise<T>(Task<T> running, TimeoutStrategy strategy, CancellationToken token)
    {
        if (strategy == TimeoutStrategy.Cooperative || running.IsCompleted)
        {
            return running;
        }

        // Pessimistic: stop waiting the moment the token fires. The abandoned task may still
        // fault later; observe it so the failure is not raised as an unobserved task exception.
        running.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return running.WaitAsync(token);
    }
}
