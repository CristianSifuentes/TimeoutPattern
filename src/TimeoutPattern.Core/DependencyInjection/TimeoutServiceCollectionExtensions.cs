using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TimeoutPattern.Core.Budgets;
using TimeoutPattern.Core.Configuration;
using TimeoutPattern.Core.Execution;
using TimeoutPattern.Core.Policies;
using TimeoutPattern.Core.Telemetry;

namespace TimeoutPattern.Core.DependencyInjection;

public static class TimeoutServiceCollectionExtensions
{
    /// <summary>Registers the timeout engine, bound to <paramref name="section"/> and validated at startup.</summary>
    public static IServiceCollection AddTimeoutPattern(this IServiceCollection services, IConfiguration section)
    {
        services.AddOptions<TimeoutOptions>().Bind(section).ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TimeoutOptions>, TimeoutOptionsValidator>());

        services.AddMetrics();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<TimeoutTelemetry>();
        services.TryAddSingleton<ITimeoutBudgetAccessor, TimeoutBudgetAccessor>();
        services.TryAddSingleton<ITimeoutPolicyResolver, TimeoutPolicyResolver>();
        services.TryAddSingleton<ITimeoutPlanner, TimeoutPlanner>();
        services.TryAddSingleton<ITimeoutExecutor, TimeoutExecutor>();

        return services;
    }
}
