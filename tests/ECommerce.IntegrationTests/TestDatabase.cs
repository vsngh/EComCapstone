using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

internal static class TestDatabase
{
    public static WebApplicationFactory<Program> CreateFactory(string dbNameSuffix)
    {
        var dbName = $"Tests_{dbNameSuffix}_{Guid.NewGuid():N}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Database:Provider"] = "InMemory",
                        ["Database:ConnectionString"] = dbName
                    });
                });
            });
    }

    public static async Task SeedAsync(WebApplicationFactory<Program> factory)
    {
        // Force host startup (runs Program.cs initializer which creates + seeds the DB).
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        if (!db.Users.Any())
        {
            ECommerce.Infrastructure.Persistence.SeedData.SeedData.Seed(db);
        }
    }
}
