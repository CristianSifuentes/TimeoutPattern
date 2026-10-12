using TimeoutPattern.Core.Policies;

namespace TimeoutPattern.Tests.Core;

public sealed class TimeoutPolicyResolverTests : IDisposable
{
    private readonly TimeoutHarness _harness = new(new Dictionary<string, string?>
    {
        ["DefaultOperationTimeout"] = "00:00:03",
        ["DefaultRequestTimeout"] = "00:00:10",
        ["Operations:pricing:Timeout"] = "00:00:01.200",
        ["Operations:pricing:Reserve"] = "00:00:00.050",
        ["Operations:pricing.tax:Strategy"] = "Pessimistic",
        ["Requests:products:Timeout"] = "00:00:02",
        ["Requests:reports:Timeout"] = "00:00:08",
        ["Requests:reports:HonorInboundDeadline"] = "false",
    });

    [Fact]
    public void Each_field_inherits_independently_from_the_nearest_ancestor()
    {
        var policy = _harness.Resolver.ResolveOperation("pricing.tax");

        Assert.Equal("pricing.tax", policy.MatchedKey);
        Assert.Equal(TimeoutStrategy.Pessimistic, policy.Strategy);   // own
        Assert.Equal(TimeSpan.FromMilliseconds(1200), policy.Timeout); // from "pricing"
        Assert.Equal(TimeSpan.FromMilliseconds(50), policy.Reserve);   // from "pricing"
    }

    [Fact]
    public void Unconfigured_child_falls_back_to_its_parent_key()
    {
        var policy = _harness.Resolver.ResolveOperation("pricing.quote.v2");

        Assert.Equal("pricing", policy.MatchedKey);
        Assert.Equal(TimeoutStrategy.Cooperative, policy.Strategy);
    }

    [Fact]
    public void Unknown_operation_gets_the_global_default()
    {
        var policy = _harness.Resolver.ResolveOperation("telemetry.flush");

        Assert.Null(policy.MatchedKey);
        Assert.Equal(TimeSpan.FromSeconds(3), policy.Timeout);
    }

    [Fact]
    public void Inbound_deadline_can_shorten_a_request()
    {
        var decision = _harness.Resolver.ResolveRequest("products.read", TimeSpan.FromMilliseconds(300));

        Assert.Equal(DeadlineSource.Inbound, decision.Source);
        Assert.Equal(TimeSpan.FromMilliseconds(300), decision.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(2), decision.PolicyTimeout);
    }

    [Fact]
    public void Inbound_deadline_can_never_lengthen_a_request()
    {
        var decision = _harness.Resolver.ResolveRequest("products.read", TimeSpan.FromMinutes(5));

        Assert.Equal(DeadlineSource.Policy, decision.Source);
        Assert.Equal(TimeSpan.FromSeconds(2), decision.Timeout);
    }

    [Fact]
    public void Policy_can_refuse_inbound_deadlines()
    {
        var decision = _harness.Resolver.ResolveRequest("reports.sales", TimeSpan.FromMilliseconds(100));

        Assert.Equal(DeadlineSource.Policy, decision.Source);
        Assert.Equal(TimeSpan.FromSeconds(8), decision.Timeout);
    }

    public void Dispose() => _harness.Dispose();
}
