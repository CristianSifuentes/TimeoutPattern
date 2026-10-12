namespace TimeoutPattern.Api.Simulation;

public sealed class SimulationOptions
{
    public const string SectionName = "Simulation";

    /// <summary>Baseline latency per dependency name, e.g. <c>"inventory": "00:00:00.150"</c>.</summary>
    public Dictionary<string, TimeSpan> Latency { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
