using System.Diagnostics;
using System.Diagnostics.Metrics;
using TimeoutPattern.Core.Execution;
using TimeoutPattern.Core.Policies;

namespace TimeoutPattern.Core.Telemetry;

/// <summary>
/// Metrics for tuning timeouts from evidence rather than guesswork.
/// The key instrument is <c>timeout_pattern.budget.utilization</c>: elapsed ÷ allowed.
/// A p99 near 1.0 means the timeout is cutting real work short; a p99 far below 1.0 means it can be tightened.
/// </summary>
public sealed class TimeoutTelemetry
{
    public const string MeterName = "TimeoutPattern";

    private readonly Counter<long> _operations;
    private readonly Histogram<double> _operationDuration;
    private readonly Histogram<double> _budgetUtilization;
    private readonly Counter<long> _requests;

    public TimeoutTelemetry(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _operations = meter.CreateCounter<long>(
            "timeout_pattern.operations", "{operation}", "Operations run under a timeout, by outcome.");
        _operationDuration = meter.CreateHistogram<double>(
            "timeout_pattern.operation.duration", "ms", "Wall-clock duration of timed operations.");
        _budgetUtilization = meter.CreateHistogram<double>(
            "timeout_pattern.budget.utilization", "1", "Elapsed time divided by the effective timeout.");
        _requests = meter.CreateCounter<long>(
            "timeout_pattern.requests", "{request}", "Requests run under a deadline, by outcome.");
    }

    public void RecordOperation(TimeoutPlan plan, TimeoutOutcome outcome, TimeSpan elapsed)
    {
        var tags = new TagList
        {
            { "operation", plan.Operation },
            { "outcome", ToTag(outcome) },
            { "bound_by", plan.BoundBy == TimeoutBound.OperationPolicy ? "policy" : "budget" },
        };

        _operations.Add(1, tags);

        if (outcome == TimeoutOutcome.Rejected)
        {
            return;
        }

        _operationDuration.Record(elapsed.TotalMilliseconds, tags);
        _budgetUtilization.Record(elapsed / plan.Effective, tags);
    }

    public void RecordRequest(RequestTimeoutDecision decision, string outcome)
    {
        _requests.Add(1, new TagList
        {
            { "policy", decision.PolicyName },
            { "deadline_source", decision.Source.ToString().ToLowerInvariant() },
            { "outcome", outcome },
        });
    }

    private static string ToTag(TimeoutOutcome outcome) => outcome switch
    {
        TimeoutOutcome.Succeeded => "succeeded",
        TimeoutOutcome.TimedOut => "timed_out",
        TimeoutOutcome.Rejected => "rejected",
        TimeoutOutcome.Cancelled => "cancelled",
        _ => "faulted",
    };
}
