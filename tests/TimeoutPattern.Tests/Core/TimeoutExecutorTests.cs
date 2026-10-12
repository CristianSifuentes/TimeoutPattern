using TimeoutPattern.Core.Budgets;
using TimeoutPattern.Core.Errors;
using TimeoutPattern.Core.Execution;

namespace TimeoutPattern.Tests.Core;

public sealed class TimeoutExecutorTests : IDisposable
{
    private readonly TimeoutHarness _harness = new(new Dictionary<string, string?>
    {
        ["MinimumViableBudget"] = "00:00:00.020",
        ["Operations:slow:Timeout"] = "00:00:01",
        ["Operations:nested:Timeout"] = "00:00:01",
        ["Operations:nested:Reserve"] = "00:00:00.100",
        ["Operations:legacy:Timeout"] = "00:00:01",
        ["Operations:legacy:Strategy"] = "Pessimistic",
    });

    [Fact]
    public async Task Returns_the_result_when_work_finishes_in_time()
    {
        var result = await _harness.Executor.ExecuteAsync("slow", _ => Task.FromResult(42));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Raises_an_operation_timeout_when_the_policy_elapses()
    {
        var running = _harness.Executor.ExecuteAsync("slow", WaitFor(TimeSpan.FromSeconds(10)));

        _harness.Clock.Advance(TimeSpan.FromSeconds(1));

        var timeout = await Assert.ThrowsAsync<OperationTimeoutException>(() => running);
        Assert.Equal(TimeoutBound.OperationPolicy, timeout.Plan.BoundBy);
        Assert.Equal("operation-policy", timeout.Diagnostic.BoundBy);
        Assert.Equal(1000, timeout.Diagnostic.EffectiveMs);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_reported_as_a_timeout()
    {
        using var caller = new CancellationTokenSource();
        var running = _harness.Executor.ExecuteAsync("slow", WaitFor(TimeSpan.FromSeconds(10)), caller.Token);

        await caller.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
    }

    [Fact]
    public async Task Enclosing_budget_minus_reserve_caps_the_operation_and_nests()
    {
        _harness.EnterRequest("request", TimeSpan.FromMilliseconds(500));
        TimeoutBudget? seen = null;

        var running = _harness.Executor.ExecuteAsync("nested", async token =>
        {
            seen = _harness.Budgets.Current;
            await Task.Delay(TimeSpan.FromSeconds(10), _harness.Clock, token);
            return 0;
        });

        Assert.NotNull(seen);
        Assert.Equal("request › nested", seen.Path);
        Assert.Equal(TimeSpan.FromMilliseconds(400), seen.Timeout); // 500 remaining − 100 reserve

        _harness.Clock.Advance(TimeSpan.FromMilliseconds(400));

        var timeout = await Assert.ThrowsAsync<OperationTimeoutException>(() => running);
        Assert.Equal(TimeoutBound.EnclosingBudget, timeout.Plan.BoundBy);
        Assert.Equal("request", _harness.Budgets.Current?.Name);
    }

    [Fact]
    public async Task Refuses_to_start_when_the_budget_is_below_the_viable_minimum()
    {
        _harness.EnterRequest("request", TimeSpan.FromMilliseconds(100));
        _harness.Clock.Advance(TimeSpan.FromMilliseconds(90));
        var started = false;

        await Assert.ThrowsAsync<BudgetExhaustedException>(() => _harness.Executor.ExecuteAsync("slow", _ =>
        {
            started = true;
            return Task.FromResult(0);
        }));

        Assert.False(started);
    }

    [Fact]
    public async Task Pessimistic_strategy_releases_the_caller_from_work_that_ignores_its_token()
    {
        var neverFinishes = new TaskCompletionSource<int>();
        var running = _harness.Executor.ExecuteAsync("legacy", _ => neverFinishes.Task);

        _harness.Clock.Advance(TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<OperationTimeoutException>(() => running);
        Assert.False(neverFinishes.Task.IsCompleted);
    }

    public void Dispose() => _harness.Dispose();

    private Func<CancellationToken, Task<int>> WaitFor(TimeSpan duration) => async token =>
    {
        await Task.Delay(duration, _harness.Clock, token);
        return 0;
    };
}
