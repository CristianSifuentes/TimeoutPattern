using System.Net.Http.Json;
using System.Text.Json;

namespace TimeoutPattern.Tests.Integration;

public sealed class SalesReportTests : IDisposable
{
    private readonly StorefrontFactory _factory = new StorefrontFactory()
        .WithSetting("Timeouts:Requests:reports.sales:Timeout", "00:00:00.600");

    [Fact]
    public async Task Short_report_completes()
    {
        var report = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/reports/sales?days=3");

        Assert.False(report.GetProperty("isPartial").GetBoolean());
        Assert.Equal(3, report.GetProperty("daysCompleted").GetInt32());
    }

    [Fact]
    public async Task Long_report_returns_partial_results_before_the_deadline()
    {
        using var response = await _factory.CreateClient().GetAsync("/api/reports/sales?days=365");

        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        var completed = report.GetProperty("daysCompleted").GetInt32();

        Assert.True(report.GetProperty("isPartial").GetBoolean());
        Assert.InRange(completed, 1, 364);
        Assert.False(string.IsNullOrEmpty(report.GetProperty("stopReason").GetString()));
    }

    public void Dispose() => _factory.Dispose();
}
