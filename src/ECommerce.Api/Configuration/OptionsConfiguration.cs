using ECommerce.Api.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Api.Configuration;

public static class OptionsConfiguration
{
    public static IServiceCollection AddApiOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.Configure<RedisOptions>(
            configuration.GetSection(RedisOptions.SectionName));

        services.Configure<PaymentsOptions>(
            configuration.GetSection(PaymentsOptions.SectionName));

        services.AddScoped<WebhookSignatureVerifier>();

        return services;
    }
}