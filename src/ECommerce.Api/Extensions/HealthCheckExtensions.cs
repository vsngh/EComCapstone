using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace ECommerce.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthChecksConfiguration(
        this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    public static IEndpointRouteBuilder MapHealthChecksConfiguration(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(
            "/health",
            new HealthCheckOptions
            {
                Predicate = _ => false
            });

        endpoints.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions());

        return endpoints;
    }
}
