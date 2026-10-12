using Microsoft.Extensions.Options;

namespace TimeoutPattern.Core.Configuration;

/// <summary>Rejects configurations that would make timeouts silently meaningless.</summary>
public sealed class TimeoutOptionsValidator : IValidateOptions<TimeoutOptions>
{
    public ValidateOptionsResult Validate(string? name, TimeoutOptions options)
    {
        var failures = new List<string>();

        RequirePositive(options.DefaultRequestTimeout, nameof(options.DefaultRequestTimeout), failures);
        RequirePositive(options.DefaultOperationTimeout, nameof(options.DefaultOperationTimeout), failures);
        RequireNonNegative(options.MinimumViableBudget, nameof(options.MinimumViableBudget), failures);
        RequireNonNegative(options.Propagation.NetworkAllowance, "Propagation.NetworkAllowance", failures);

        if (string.IsNullOrWhiteSpace(options.Propagation.HeaderName))
        {
            failures.Add("Propagation.HeaderName must not be empty.");
        }

        foreach (var (key, policy) in options.Requests)
        {
            if (policy.Timeout is { } timeout)
            {
                RequirePositive(timeout, $"Requests:{key}:Timeout", failures);
            }
        }

        foreach (var (key, policy) in options.Operations)
        {
            if (policy.Timeout is { } timeout)
            {
                RequirePositive(timeout, $"Operations:{key}:Timeout", failures);
            }

            if (policy.Reserve is { } reserve)
            {
                RequireNonNegative(reserve, $"Operations:{key}:Reserve", failures);
            }

            if (policy is { Timeout: { } t, Reserve: { } r } && r >= t)
            {
                failures.Add($"Operations:{key}:Reserve ({r}) must be smaller than its Timeout ({t}).");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void RequirePositive(TimeSpan value, string path, List<string> failures)
    {
        if (value <= TimeSpan.Zero)
        {
            failures.Add($"{path} must be greater than zero (was {value}).");
        }
    }

    private static void RequireNonNegative(TimeSpan value, string path, List<string> failures)
    {
        if (value < TimeSpan.Zero)
        {
            failures.Add($"{path} must not be negative (was {value}).");
        }
    }
}
