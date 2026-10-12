using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeoutPattern.Infrastructure.Http;

namespace TimeoutPattern.Tests.Integration;

/// <summary>
/// Hosts the API in memory. The warehouse client is routed back into the same test server,
/// so deadline propagation goes over real HTTP without opening a port.
/// </summary>
public sealed class StorefrontFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _settings = new()
    {
        ["Simulation:Latency:catalog"] = "00:00:00.005",
        ["Simulation:Latency:demand"] = "00:00:00.005",
        ["Simulation:Latency:tax"] = "00:00:00.005",
        ["Simulation:Latency:inventory"] = "00:00:00.005",
        ["Simulation:Latency:ledger"] = "00:00:00.020",
    };

    public StorefrontFactory WithSetting(string key, string value)
    {
        _settings[key] = value;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));
        builder.ConfigureServices(services => services
            .AddHttpClient(WarehouseOptions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler()));
    }
}
