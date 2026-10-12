namespace TimeoutPattern.Core.Forecasting;

/// <summary>
/// Exponentially weighted mean and variance of observed durations (Finch, 2009, incremental form).
/// Recent samples weigh more, so the estimate follows a dependency that is slowing down.
/// Use <see cref="UpperBound"/> to decide whether one more iteration fits in what is left of a budget.
/// </summary>
public sealed class EwmaDurationEstimator
{
    private readonly double _alpha;
    private double _mean;
    private double _variance;

    /// <param name="alpha">Weight of the newest sample, in (0, 1]. Higher reacts faster and is noisier.</param>
    public EwmaDurationEstimator(double alpha = 0.3)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alpha);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alpha, 1.0);
        _alpha = alpha;
    }

    public int Samples { get; private set; }

    public TimeSpan Mean => TimeSpan.FromMilliseconds(_mean);

    public TimeSpan StandardDeviation => TimeSpan.FromMilliseconds(Math.Sqrt(_variance));

    public void Observe(TimeSpan duration)
    {
        var sample = duration.TotalMilliseconds;

        if (Samples++ == 0)
        {
            _mean = sample;
            return;
        }

        var deviation = sample - _mean;
        var increment = _alpha * deviation;
        _mean += increment;
        _variance = (1 - _alpha) * (_variance + deviation * increment);
    }

    /// <summary>Mean plus <paramref name="sigmas"/> standard deviations: a cautious estimate of the next sample.</summary>
    public TimeSpan UpperBound(double sigmas = 2.0) =>
        TimeSpan.FromMilliseconds(_mean + sigmas * Math.Sqrt(_variance));
}
