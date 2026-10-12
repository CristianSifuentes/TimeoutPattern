namespace TimeoutPattern.Core.Policies;

public interface ITimeoutPolicyResolver
{
    OperationPolicy ResolveOperation(string operation);

    RequestTimeoutDecision ResolveRequest(string? policyName, TimeSpan? inboundDeadline);
}
