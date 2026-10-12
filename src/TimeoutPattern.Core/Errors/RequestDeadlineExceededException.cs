using System.Globalization;
using TimeoutPattern.Core.Policies;

namespace TimeoutPattern.Core.Errors;

public sealed class RequestDeadlineExceededException(RequestTimeoutDecision decision, TimeSpan elapsed, Exception? innerException)
    : DeadlineException(Describe(decision, elapsed), TimeoutDiagnostic.ForRequest(decision, elapsed), innerException)
{
    public RequestTimeoutDecision Decision { get; } = decision;

    private static string Describe(RequestTimeoutDecision decision, TimeSpan elapsed)
    {
        var source = decision.Source == DeadlineSource.Inbound ? "set by the caller" : "set by policy";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Request '{decision.PolicyName}' exceeded its {decision.Timeout.TotalMilliseconds:F0} ms deadline ({source}) after {elapsed.TotalMilliseconds:F0} ms.");
    }
}
