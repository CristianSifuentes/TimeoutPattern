using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeoutPattern.Application.Abstractions;
using TimeoutPattern.Infrastructure.Http;
using TimeoutPattern.Infrastructure.Simulation;

namespace TimeoutPattern.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddStorefrontInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WarehouseOptions>()
            .Bind(configuration.GetSection(WarehouseOptions.SectionName))
            .Validate(options => options.BaseUrl is { IsAbsoluteUri: true }, "Warehouse:BaseUrl must be an absolute URL.")
            .ValidateOnStart();

        services.AddSingleton<ICatalog, InMemoryCatalog>();
        services.AddSingleton<IDemandModel, MonteCarloDemandModel>();
        services.AddSingleton<ITaxCalculator, LegacyTaxCalculator>();
        services.AddSingleton<ISalesLedger, SimulatedSalesLedger>();

        services.AddTransient<DeadlinePropagationHandler>();
        services
            .AddHttpClient<IInventoryGateway, InventoryHttpGateway>(WarehouseOptions.HttpClientName, (provider, client) =>
            {
                client.BaseAddress = provider.GetRequiredService<IOptions<WarehouseOptions>>().Value.BaseUrl;

                // The timeout executor owns this call's deadline. HttpClient's 100-second
                // default would never fire first; it would only make failures harder to read.
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddHttpMessageHandler<DeadlinePropagationHandler>();

        return services;
    }
}
