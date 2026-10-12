using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TimeoutPattern.Api.Timeouts;
using TimeoutPattern.Core.Configuration;
using TimeoutPattern.Core.Execution;

namespace TimeoutPattern.Api.Controllers;

/// <summary>Shows how timeouts resolve right now, including changes picked up by configuration reload.</summary>
[ApiController]
[Route("api/timeouts")]
[TimeoutPolicy("diagnostics")]
public sealed class TimeoutsController(IOptionsMonitor<TimeoutOptions> options, ITimeoutPlanner planner) : ControllerBase
{
    [HttpGet]
    public TimeoutOptions Configuration() => options.CurrentValue;

    /// <summary>What <paramref name="operation"/> would be allowed if it started now, inside this request.</summary>
    [HttpGet("plan/{operation}")]
    public PlanResponse Plan(string operation)
    {
        var plan = planner.Plan(operation);

        return new PlanResponse(
            plan.Operation,
            plan.Policy.MatchedKey,
            plan.Policy.Strategy.ToString(),
            plan.Policy.Timeout.TotalMilliseconds,
            plan.Policy.Reserve.TotalMilliseconds,
            Math.Round(plan.Effective.TotalMilliseconds, 1),
            plan.BoundBy.ToString(),
            plan.Path,
            plan.IsViable);
    }

    public sealed record PlanResponse(
        string Operation,
        string? MatchedPolicy,
        string Strategy,
        double ConfiguredMs,
        double ReserveMs,
        double EffectiveMs,
        string BoundBy,
        string Path,
        bool IsViable);
}
