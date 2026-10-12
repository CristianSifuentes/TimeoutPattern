using Microsoft.AspNetCore.Diagnostics;
using TimeoutPattern.Core.Errors;

namespace TimeoutPattern.Api.Timeouts;

/// <summary>
/// Fallback for timeouts raised outside <see cref="DeadlineMiddleware"/>, such as on
/// <see cref="UnboundedRequestAttribute"/> endpoints. Timeouts inside a deadline are answered
/// by the middleware, which keeps them out of the error log.
/// </summary>
internal sealed class DeadlineExceptionHandler(DeadlineProblemWriter writer) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) =>
        exception is DeadlineException deadline && await writer.TryWriteAsync(httpContext, deadline);
}
