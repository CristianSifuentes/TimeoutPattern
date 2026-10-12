using TimeoutPattern.Core.Execution;
using TimeoutPattern.Core.Policies;

namespace TimeoutPattern.Core.Errors;

/// <summary>Serializable account of a timeout, suitable for a problem-details response.</summary>
public sealed record TimeoutDiagnostic(
    string Scope,
    string Name,
    string Path,
    string BoundBy,
    double ConfiguredMs,
    double EffectiveMs,
    double ElapsedMs)
{
    internal static TimeoutDiagnostic ForOperation(TimeoutPlan plan, TimeSpan elapsed) => new(
        Scope: "operation",
        Name: plan.Operation,
        Path: plan.Path,
        BoundBy: plan.BoundBy == TimeoutBound.OperationPolicy ? "operation-policy" : "enclosing-budget",
        ConfiguredMs: Round(plan.Policy.Timeout),
        EffectiveMs: Round(plan.Effective),
        ElapsedMs: Round(elapsed));

    internal static TimeoutDiagnostic ForRequest(RequestTimeoutDecision decision, TimeSpan elapsed) => new(
        Scope: "request",
        Name: decision.PolicyName,
        Path: decision.PolicyName,
        BoundBy: decision.Source switch
        {
            DeadlineSource.Inbound => "inbound-deadline",
            DeadlineSource.Policy => "request-policy",
            _ => "default-policy",
        },
        ConfiguredMs: Round(decision.PolicyTimeout),
        EffectiveMs: Round(decision.Timeout),
        ElapsedMs: Round(elapsed));

    private static double Round(TimeSpan value) => Math.Round(value.TotalMilliseconds, 1);
}
