namespace TimeoutPattern.Application.Abstractions;

/// <summary>How slow a simulated dependency should be right now. Lets the demo create timeouts on demand.</summary>
public interface ILatencySimulator
{
    TimeSpan LatencyFor(string dependency);
}
