using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.Products;
using ECommerce.Application.Products.DTOs;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public class ProductCatalogTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _adminClient = default!;

    public ProductCatalogTests()
    {
        _factory = TestDatabase.CreateFactory("ProductCatalog");
    }

    public async Task InitializeAsync()
    {
        await TestDatabase.SeedAsync(_factory);
        var token = await AuthHelper.EnsureAdminTokenAsync(_factory);
        _adminClient = AuthHelper.CreateClientWithToken(_factory, token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetProducts_Returns_Paginated_List()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products?page=1&pageSize=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(3, body.PageSize);
        Assert.True(body.TotalCount >= 3);
        Assert.True(body.TotalPages >= 1);
        Assert.Equal(3, body.Items.Count);
    }

    [Fact]
    public async Task GetProducts_Search_Filters_ByName()
    {
        var client = _factory.CreateClient();

        var product = GetFirstProduct();

        var response = await client.GetAsync(
            $"/api/products?search={Uri.EscapeDataString(product.Name)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(body);
        Assert.True(body.Items.Count >= 1);
        Assert.Contains(body.Items, p => p.Id == product.Id);
    }

    [Fact]
    public async Task GetProduct_By_Id_Returns_Product()
    {
        var client = _factory.CreateClient();
        var product = GetFirstProduct();

        var response = await client.GetAsync($"/api/products/{product.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(body);
        Assert.Equal(product.Id, body.Id);
        Assert.Equal(product.Sku, body.Sku);
        Assert.True(body.Price > 0);
    }

    [Fact]
    public async Task GetProduct_Unknown_Id_Returns_NotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProductService_Can_Create_Update_And_Delete()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ProductService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        var categoryId = dbContext.Categories.First().Id;

        var created = await service.CreateAsync(
            new CreateProductRequest(
                "Test Product",
                $"SKU-{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
                19.99m,
                categoryId,
                "A test product"),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.Id);

        var fetched = await service.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal("Test Product", fetched.Name);

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateProductRequest("Renamed Product", 24.99m, "Updated"),
            CancellationToken.None);

        Assert.Equal("Renamed Product", updated.Name);
        Assert.Equal(24.99m, updated.Price);

        await service.DeleteAsync(created.Id, CancellationToken.None);

        var deleted = await service.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task CreateProduct_With_Duplicate_Sku_Throws()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ProductService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        var existing = dbContext.Products.First();
        var categoryId = dbContext.Categories.First().Id;

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await service.CreateAsync(
                new CreateProductRequest(
                    "Duplicate",
                    existing.Sku,
                    10m,
                    categoryId,
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateProduct_With_Invalid_Data_Returns_BadRequest()
    {
        var response = await _adminClient.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest("", "  ", -5m, Guid.Empty, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Product_Then_Update_Reflects_Change()
    {
        // Exercises the detail-cache read + invalidation path for products.
        var product = GetFirstProduct();
        var anonClient = _factory.CreateClient();

        var first = await anonClient.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var before = await first.Content.ReadFromJsonAsync<ProductResponse>();

        var updateResp = await _adminClient.PutAsJsonAsync(
            $"/api/products/{product.Id}",
            new UpdateProductRequest(product.Name, product.Price + 1, "Updated cached product"));

        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        var second = await anonClient.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var after = await second.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.Equal(before!.Price + 1, after!.Price);
        Assert.Equal("Updated cached product", after!.Description);
    }

    private ProductResponse GetFirstProduct()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        var product = dbContext.Products.First();
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Sku,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.IsActive,
            product.CategoryId,
            product.Category?.Name ?? string.Empty);
    }
}