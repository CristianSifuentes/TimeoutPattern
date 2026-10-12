using TimeoutPattern.Core.Configuration;

namespace TimeoutPattern.Tests.Core;

public sealed class TimeoutOptionsValidatorTests
{
    private readonly TimeoutOptionsValidator _validator = new();

    [Fact]
    public void Accepts_sane_configuration()
    {
        var options = new TimeoutOptions();
        options.Operations["db"] = new OperationPolicyOptions { Timeout = TimeSpan.FromSeconds(1), Reserve = TimeSpan.FromMilliseconds(50) };

        Assert.True(_validator.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Rejects_a_reserve_that_swallows_the_whole_timeout()
    {
        var options = new TimeoutOptions();
        options.Operations["db"] = new OperationPolicyOptions { Timeout = TimeSpan.FromSeconds(1), Reserve = TimeSpan.FromSeconds(1) };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Operations:db:Reserve", result.FailureMessage);
    }

    [Fact]
    public void Rejects_non_positive_timeouts()
    {
        var options = new TimeoutOptions { DefaultRequestTimeout = TimeSpan.Zero };
        options.Requests["api"] = new RequestPolicyOptions { Timeout = TimeSpan.FromSeconds(-1) };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Equal(2, result.Failures!.Count());
    }
}
