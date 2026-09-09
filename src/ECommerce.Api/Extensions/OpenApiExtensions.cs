using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace ECommerce.Api.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiDocumentation(
        this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "ECommerce API";
                document.Info.Version = "v1";
                document.Info.Description =
                    "Production-style e-commerce backend built with ASP.NET Core and Clean Architecture.";

                var securityScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Description = "Enter the JWT token as: Bearer {token}"
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = securityScheme;

                var bearerRequirement = new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
                };

                document.Security ??= [];
                document.Security.Add(bearerRequirement);

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static IEndpointRouteBuilder MapOpenApiDocumentation(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpenApi();

        var app = (WebApplication)endpoints;
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "ECommerce API v1");
            options.DocumentTitle = "ECommerce API";
            options.HeadContent = "<style>#swagger-ui .topbar { display: none; }</style>";
        });

        return endpoints;
    }
}
