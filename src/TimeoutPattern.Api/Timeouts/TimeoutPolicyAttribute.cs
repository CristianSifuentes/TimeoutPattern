namespace TimeoutPattern.Api.Timeouts;

/// <summary>
/// Names the request policy under <c>Timeouts:Requests</c> that sets this endpoint's deadline.
/// An attribute on an action overrides one on its controller.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class TimeoutPolicyAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

/// <summary>Opts an endpoint out of request deadlines, e.g. a long-lived stream that manages its own lifetime.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class UnboundedRequestAttribute : Attribute;
