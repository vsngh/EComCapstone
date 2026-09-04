using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public class DatabaseTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public DatabaseTests()
    {
        _factory = TestDatabase.CreateFactory("Database");
    }

    public async Task InitializeAsync()
    {
        await TestDatabase.SeedAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void Database_Is_Created_And_Seeded()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        Assert.True(dbContext.Products.Any());
        Assert.True(dbContext.Users.Any());
        Assert.True(dbContext.Categories.Any());
        Assert.True(dbContext.Inventory.Any());
    }

    [Fact]
    public void Seed_Contains_Admin_And_Customer()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        var emails = dbContext.Users
            .ToList()
            .Select(u => u.Email.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("admin@ecommerce.com", emails);
        Assert.Contains("customer@ecommerce.com", emails);
    }

    [Fact]
    public void Seed_Products_Have_Inventory()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        foreach (var product in dbContext.Products.ToList())
        {
            var inventory = dbContext.Inventory.SingleOrDefault(i => i.ProductId == product.Id);
            Assert.NotNull(inventory);
            Assert.True(inventory.AvailableQuantity > 0);
        }
    }
}