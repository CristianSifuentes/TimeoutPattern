using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TimeoutPattern.Tests.Integration;

public sealed class StorefrontTimeoutTests : IClassFixture<StorefrontFactory>
{
    private readonly HttpClient _client;

    public StorefrontTimeoutTests(StorefrontFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Healthy_request_carries_its_deadline_in_response_headers()
    {
        using var response = await _client.GetAsync("/api/products/TEL-080");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("deadline;desc=\"products.read (policy)\"", response.Headers.GetValues("Server-Timing").Single());
        Assert.Equal("products.read", response.Headers.GetValues("X-Timeout-Policy").Single());

        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("in-stock", page.GetProperty("stock").GetProperty("status").GetString());
        Assert.Equal(0, page.GetProperty("degradations").GetArrayLength());
    }

    [Fact]
    public async Task Slow_optional_dependency_degrades_the_page_instead_of_failing_it()
    {
        using var response = await SendAsync("/api/products/TEL-080", latency: "inventory=3000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("unknown", page.GetProperty("stock").GetProperty("status").GetString());
        Assert.Equal("inventory", page.GetProperty("degradations")[0].GetProperty("dependency").GetString());
    }

    [Fact]
    public async Task Slow_required_dependency_fails_fast_with_a_descriptive_504()
    {
        var timer = Stopwatch.StartNew();
        using var response = await SendAsync("/api/products/TEL-080", latency: "catalog=3000");

        var timeout = await ReadTimeoutAsync(response);
        Assert.Equal("catalog.read", timeout.GetProperty("name").GetString());
        Assert.Equal("operation-policy", timeout.GetProperty("boundBy").GetString());
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2), $"Took {timer.Elapsed}");
    }

    [Fact]
    public async Task Pessimistic_strategy_bounds_a_dependency_that_ignores_cancellation()
    {
        var timer = Stopwatch.StartNew();
        using var response = await SendAsync("/api/products/TEL-080", latency: "tax=3000");

        var timeout = await ReadTimeoutAsync(response);
        Assert.Equal("products.read › pricing.quote › pricing.tax", timeout.GetProperty("path").GetString());
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2), $"Took {timer.Elapsed}");
    }

    [Fact]
    public async Task Inbound_deadline_tightens_every_nested_budget()
    {
        var timer = Stopwatch.StartNew();
        using var response = await SendAsync("/api/products/TEL-080", latency: "demand=2000", deadlineMs: 150);

        var timeout = await ReadTimeoutAsync(response);
        Assert.Equal("enclosing-budget", timeout.GetProperty("boundBy").GetString());
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(1), $"Took {timer.Elapsed}");
    }

    [Fact]
    public async Task Downstream_service_gives_up_at_the_propagated_deadline()
    {
        using var response = await SendAsync("/api/warehouse/stock/TEL-080?latencyMs=3000", deadlineMs: 100);

        var timeout = await ReadTimeoutAsync(response);
        Assert.Equal("request", timeout.GetProperty("scope").GetString());
        Assert.Equal("inbound-deadline", timeout.GetProperty("boundBy").GetString());
    }

    [Fact]
    public async Task Unknown_product_is_a_plain_404()
    {
        using var response = await _client.GetAsync("/api/products/NOPE");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Diagnostics_show_field_level_inheritance()
    {
        var plan = await _client.GetFromJsonAsync<JsonElement>("/api/timeouts/plan/pricing.tax");

        Assert.Equal("pricing.tax", plan.GetProperty("matchedPolicy").GetString());
        Assert.Equal("Pessimistic", plan.GetProperty("strategy").GetString());
        Assert.Equal(50, plan.GetProperty("reserveMs").GetDouble()); // inherited from "pricing"
    }

    private async Task<HttpResponseMessage> SendAsync(string uri, string? latency = null, int? deadlineMs = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (latency is not null)
        {
            request.Headers.Add("X-Simulate-Latency", latency);
        }

        if (deadlineMs is not null)
        {
            request.Headers.Add("X-Request-Timeout-Ms", deadlineMs.Value.ToString());
        }

        return await _client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadTimeoutAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("timeout");
    }
}
