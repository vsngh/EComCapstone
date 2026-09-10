using ECommerce.Api.Configuration;
using ECommerce.Api.Extensions;
using ECommerce.Api.Hubs;
using ECommerce.Api.Notifications;
using ECommerce.Application;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddApiOptions(builder.Configuration);

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthenticationConfiguration();

builder.Services.AddSignalR();

builder.Services.AddScoped<IOrderNotifier, OrderNotifier>();

builder.Services.AddLogging();

builder.Services.AddOpenApiDocumentation();

builder.Services.AddHealthChecksConfiguration();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.UseExceptionHandling();

app.UseRequestLogging();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<OrderHub>("/hubs/orders");

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApiDocumentation();
}

app.MapHealthChecksConfiguration();

app.Run();

public partial class Program;
