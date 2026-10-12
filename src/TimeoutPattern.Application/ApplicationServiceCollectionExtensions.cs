using Microsoft.Extensions.DependencyInjection;
using TimeoutPattern.Application.Reports;
using TimeoutPattern.Application.Storefront;

namespace TimeoutPattern.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddStorefront(this IServiceCollection services)
    {
        services.AddScoped<PricingService>();
        services.AddScoped<ProductPageService>();
        services.AddScoped<SalesReportService>();
        return services;
    }
}
