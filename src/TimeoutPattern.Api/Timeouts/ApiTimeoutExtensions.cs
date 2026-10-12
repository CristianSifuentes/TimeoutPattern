namespace TimeoutPattern.Api.Timeouts;

internal static class ApiTimeoutExtensions
{
    public static IServiceCollection AddDeadlineResponses(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddSingleton<DeadlineProblemWriter>();
        services.AddExceptionHandler<DeadlineExceptionHandler>();
        return services;
    }

    /// <summary>Must run after routing, so the endpoint's <see cref="TimeoutPolicyAttribute"/> is known.</summary>
    public static IApplicationBuilder UseDeadlines(this IApplicationBuilder app) =>
        app.UseMiddleware<DeadlineMiddleware>();
}
