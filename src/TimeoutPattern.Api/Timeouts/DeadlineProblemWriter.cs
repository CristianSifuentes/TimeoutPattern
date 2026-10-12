using Microsoft.AspNetCore.Mvc;
using TimeoutPattern.Core.Errors;

namespace TimeoutPattern.Api.Timeouts;

/// <summary>
/// Writes a <see cref="DeadlineException"/> as a 504 problem response that says which deadline
/// ran out, how much time it had, and which limit was the tighter one.
/// </summary>
internal sealed class DeadlineProblemWriter(IProblemDetailsService problemDetails)
{
    private const string GatewayTimeoutType = "https://www.rfc-editor.org/rfc/rfc9110#name-504-gateway-timeout";

    public async ValueTask<bool> TryWriteAsync(HttpContext httpContext, DeadlineException deadline)
    {
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = deadline,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Type = GatewayTimeoutType,
                Title = deadline switch
                {
                    RequestDeadlineExceededException => "Request deadline exceeded",
                    BudgetExhaustedException => "Not enough time left to start",
                    _ => "Dependency timed out",
                },
                Detail = deadline.Message,
                Extensions = { ["timeout"] = deadline.Diagnostic },
            },
        });
    }
}
