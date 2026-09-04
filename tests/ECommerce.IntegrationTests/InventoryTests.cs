using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.Inventory.DTOs;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public class InventoryTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _adminClient = default!;

    public InventoryTests()
    {
        _factory = TestDatabase.CreateFactory("Inventory");
    }

    public async Task InitializeAsync()
    {
        await TestDatabase.SeedAsync(_factory);
        var token = await AuthHelper.EnsureAdminTokenAsync(_factory);
        _adminClient = AuthHelper.CreateClientWithToken(_factory, token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private Guid GetFirstProductId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        return db.Products.First().Id;
    }

    private void ResetInventory()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        foreach (var inv in db.Inventory)
        {
            if (inv.ReservedQuantity > 0)
            {
                inv.Release(inv.ReservedQuantity);
            }
            inv.SetAvailableQuantity(100);
        }
        db.SaveChanges();
    }

    [Fact]
    public async Task Get_Inventory_Returns_Quantities()
    {
        ResetInventory();
        var productId = GetFirstProductId();

        var response = await _adminClient.GetAsync($"/api/products/{productId}/inventory");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var inv = await response.Content.ReadFromJsonAsync<InventoryResponse>();
        Assert.NotNull(inv);
        Assert.Equal(100, inv.AvailableQuantity);
        Assert.Equal(0, inv.ReservedQuantity);
    }

    [Fact]
    public async Task Add_Stock_Increases_Available()
    {
        ResetInventory();
        var productId = GetFirstProductId();

        var response = await _adminClient.PostAsJsonAsync(
            $"/api/products/{productId}/inventory/add",
            new AddStockRequest(50));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var inv = await response.Content.ReadFromJsonAsync<InventoryResponse>();
        Assert.NotNull(inv);
        Assert.Equal(150, inv.AvailableQuantity);
    }

    [Fact]
    public async Task Reserve_Stock_Moves_To_Reserved()
    {
        ResetInventory();
        var productId = GetFirstProductId();

        var response = await _adminClient.PostAsJsonAsync(
            $"/api/products/{productId}/inventory/reserve",
            new ReserveStockRequest(20));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var inv = await response.Content.ReadFromJsonAsync<InventoryResponse>();
        Assert.NotNull(inv);
        Assert.Equal(80, inv.AvailableQuantity);
        Assert.Equal(20, inv.ReservedQuantity);
    }

    [Fact]
    public async Task Reserve_More_Than_Available_Returns_Conflict()
    {
        ResetInventory();
        var productId = GetFirstProductId();

        var response = await _adminClient.PostAsJsonAsync(
            $"/api/products/{productId}/inventory/reserve",
            new ReserveStockRequest(200));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Release_Stock_Returns_To_Available()
    {
        ResetInventory();
        var productId = GetFirstProductId();

        await _adminClient.PostAsJsonAsync(
            $"/api/products/{productId}/inventory/reserve",
            new ReserveStockRequest(20));

        var response = await _adminClient.PostAsJsonAsync(
            $"/api/products/{productId}/inventory/release",
            new ReleaseStockRequest(20));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var inv = await response.Content.ReadFromJsonAsync<InventoryResponse>();
        Assert.NotNull(inv);
        Assert.Equal(100, inv.AvailableQuantity);
        Assert.Equal(0, inv.ReservedQuantity);
    }

    [Fact]
    public async Task Add_Stock_Negative_Returns_BadRequest()
    {
        var productId = GetFirstProductId();

        var response = await _adminClient.PostAsJsonAsync(
            $"/api/products/{productId}/inventory/add",
            new AddStockRequest(-5));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Non_Admin_Cannot_Access_Inventory()
    {
        var (client, _) = await AuthHelper.NewCustomerAsync(_factory);
        var productId = GetFirstProductId();

        var response = await client.GetAsync($"/api/products/{productId}/inventory");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
