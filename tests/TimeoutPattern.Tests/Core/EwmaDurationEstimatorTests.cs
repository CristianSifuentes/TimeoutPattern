using TimeoutPattern.Core.Forecasting;

namespace TimeoutPattern.Tests.Core;

public sealed class EwmaDurationEstimatorTests
{
    [Fact]
    public void Steady_samples_converge_with_no_spread()
    {
        var estimator = new EwmaDurationEstimator();

        for (var i = 0; i < 20; i++)
        {
            estimator.Observe(TimeSpan.FromMilliseconds(100));
        }

        Assert.Equal(100, estimator.Mean.TotalMilliseconds, precision: 6);
        Assert.Equal(estimator.Mean, estimator.UpperBound());
    }

    [Fact]
    public void Jitter_widens_the_upper_bound_above_the_mean()
    {
        var estimator = new EwmaDurationEstimator();

        foreach (var ms in new[] { 80, 120, 90, 140, 70, 130 })
        {
            estimator.Observe(TimeSpan.FromMilliseconds(ms));
        }

        Assert.True(estimator.StandardDeviation > TimeSpan.Zero);
        Assert.True(estimator.UpperBound() > estimator.Mean);
    }

    [Fact]
    public void A_slowing_dependency_pulls_the_mean_toward_recent_samples()
    {
        var estimator = new EwmaDurationEstimator(alpha: 0.5);

        estimator.Observe(TimeSpan.FromMilliseconds(100));
        estimator.Observe(TimeSpan.FromMilliseconds(300));

        Assert.Equal(200, estimator.Mean.TotalMilliseconds, precision: 6);
    }
}
