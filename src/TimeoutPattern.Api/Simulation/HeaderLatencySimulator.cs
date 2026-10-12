using System.Globalization;
using Microsoft.Extensions.Options;
using TimeoutPattern.Application.Abstractions;

namespace TimeoutPattern.Api.Simulation;

/// <summary>
/// Configured baseline latency, overridable per request with a header such as
/// <c>X-Simulate-Latency: inventory=2500, tax=900</c> (milliseconds). Lets you make any
/// dependency slow for one request and watch how the deadlines respond.
/// </summary>
internal sealed class HeaderLatencySimulator(
    IHttpContextAccessor httpContextAccessor,
    IOptionsMonitor<SimulationOptions> options) : ILatencySimulator
{
    public const string HeaderName = "X-Simulate-Latency";

    public TimeSpan LatencyFor(string dependency) =>
        ReadOverride(dependency) ?? options.CurrentValue.Latency.GetValueOrDefault(dependency);

    private TimeSpan? ReadOverride(string dependency)
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrEmpty(header))
        {
            return null;
        }

        foreach (var entry in header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2
                && parts[0].Equals(dependency, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds))
            {
                return TimeSpan.FromMilliseconds(milliseconds);
            }
        }

        return null;
    }
}
