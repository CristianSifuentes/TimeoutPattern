using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TimeoutPattern.Core.Budgets;
using TimeoutPattern.Core.DependencyInjection;
using TimeoutPattern.Core.Execution;
using TimeoutPattern.Core.Policies;

namespace TimeoutPattern.Tests.Core;

/// <summary>The real container wiring, on a clock the test controls.</summary>
internal sealed class TimeoutHarness : IDisposable
{
    private readonly ServiceProvider _provider;

    public TimeoutHarness(IDictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        _provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(Clock)
            .AddTimeoutPattern(configuration)
            .BuildServiceProvider();
    }

    public FakeTimeProvider Clock { get; } = new();

    public ITimeoutExecutor Executor => _provider.GetRequiredService<ITimeoutExecutor>();

    public ITimeoutPlanner Planner => _provider.GetRequiredService<ITimeoutPlanner>();

    public ITimeoutPolicyResolver Resolver => _provider.GetRequiredService<ITimeoutPolicyResolver>();

    public ITimeoutBudgetAccessor Budgets => _provider.GetRequiredService<ITimeoutBudgetAccessor>();

    public TimeoutBudget EnterRequest(string name, TimeSpan timeout) =>
        Budgets.Current = TimeoutBudget.ForRequest(name, timeout, Clock);

    public void Dispose() => _provider.Dispose();
}
