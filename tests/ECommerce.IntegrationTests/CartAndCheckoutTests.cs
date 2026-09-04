using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.Carts.DTOs;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.DTOs;
using ECommerce.Infrastructure.Notifications;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public class CartAndCheckoutTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public CartAndCheckoutTests()
    {
        _factory = TestDatabase.CreateFactory("CartCheckout");
    }

    public async Task InitializeAsync()
    {
        await TestDatabase.SeedAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(HttpClient Client, Guid UserId, Guid ProductId)> NewCustomerAsync()
    {
        var (client, userId) = await AuthHelper.NewCustomerAsync(_factory);
        var productId = GetFirstProductId();
        return (client, userId, productId);
    }

    private Guid GetFirstProductId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        return db.Products.First().Id;
    }

    private void ResetDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        db.CartItems.RemoveRange(db.CartItems);
        db.Carts.RemoveRange(db.Carts);
        db.Orders.RemoveRange(db.Orders);
        db.Payments.RemoveRange(db.Payments);
        db.SaveChanges();
    }

    [Fact]
    public async Task Get_Empty_Cart_Returns_NotFound()
    {
        ResetDatabase();
        var (client, _, _) = await NewCustomerAsync();

        var response = await client.GetAsync("/api/cart");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Add_Item_To_Cart()
    {
        ResetDatabase();
        var (client, _, productId) = await NewCustomerAsync();

        var response = await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 2));

        response.EnsureSuccessStatusCode();

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);
        Assert.Single(cart.Items);
        Assert.Equal(productId, cart.Items[0].ProductId);
        Assert.Equal(2, cart.Items[0].Quantity);
    }

    [Fact]
    public async Task Add_Same_Product_Increases_Quantity()
    {
        ResetDatabase();
        var (client, _, productId) = await NewCustomerAsync();

        await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 2));

        var response = await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 3));

        response.EnsureSuccessStatusCode();

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);
        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items[0].Quantity);
    }

    [Fact]
    public async Task Update_Cart_Item_Quantity()
    {
        ResetDatabase();
        var (client, _, productId) = await NewCustomerAsync();

        await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 2));

        var response = await client.PutAsJsonAsync(
            $"/api/cart/items/{productId}",
            new UpdateCartItemQuantityRequest(10));

        response.EnsureSuccessStatusCode();

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);
        Assert.Equal(10, cart.Items[0].Quantity);
    }

    [Fact]
    public async Task Remove_Item_From_Cart()
    {
        ResetDatabase();
        var (client, _, productId) = await NewCustomerAsync();

        await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 2));

        var response = await client.DeleteAsync($"/api/cart/items/{productId}");

        response.EnsureSuccessStatusCode();

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task Clear_Cart()
    {
        ResetDatabase();
        var (client, _, productId) = await NewCustomerAsync();

        await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 2));

        var response = await client.DeleteAsync("/api/cart");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_Creates_Order_With_Correct_Status()
    {
        ResetDatabase();
        var (client, _, productId) = await NewCustomerAsync();

        await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 3));

        var checkoutRequest = new CheckoutRequest(
            "123 Main St",
            null,
            "Mumbai",
            "Maharashtra",
            "400001",
            "India");

        var response = await client.PostAsJsonAsync("/api/checkout", checkoutRequest);

        response.EnsureSuccessStatusCode();

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.StartsWith("ORD-", order.OrderNumber);
        Assert.Single(order.Items);
        Assert.Equal(3, order.Items[0].Quantity);
        Assert.True(order.TotalAmount > 0);
    }

    [Fact]
    public async Task Checkout_Empty_Cart_Returns_BadRequest()
    {
        ResetDatabase();
        var (client, _, productId) = await NewCustomerAsync();

        // Create an empty cart (add then remove an item)
        await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 1));
        await client.DeleteAsync($"/api/cart/items/{productId}");

        var checkoutRequest = new CheckoutRequest(
            "123 Main St",
            null,
            "Mumbai",
            "Maharashtra",
            "400001",
            "India");

        var response = await client.PostAsJsonAsync("/api/checkout", checkoutRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_Disables_Cart()
    {
        ResetDatabase();
        var (client, _, productId) = await NewCustomerAsync();

        await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 1));

        await client.PostAsJsonAsync(
            "/api/checkout",
            new CheckoutRequest("123 Main St", null, "Mumbai", "Maharashtra", "400001", "India"));

        var cartResponse = await client.GetAsync("/api/cart");
        Assert.Equal(HttpStatusCode.NotFound, cartResponse.StatusCode);
    }

    [Fact]
    public async Task Checkout_With_Insufficient_Stock_Returns_Conflict()
    {
        ResetDatabase();
        var (client, _, productId) = await NewCustomerAsync();

        await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 200));

        var response = await client.PostAsJsonAsync(
            "/api/checkout",
            new CheckoutRequest("123 Main St", null, "Mumbai", "Maharashtra", "400001", "India"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_Orders_After_Checkout()
    {
        ResetDatabase();
        var (client, _, productId) = await NewCustomerAsync();

        await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 1));

        await client.PostAsJsonAsync(
            "/api/checkout",
            new CheckoutRequest("123 Main St", null, "Mumbai", "Maharashtra", "400001", "India"));

        var response = await client.GetAsync("/api/orders");

        response.EnsureSuccessStatusCode();

        var orders = await response.Content.ReadFromJsonAsync<List<OrderResponse>>();
        Assert.NotNull(orders);
        Assert.Single(orders);
    }

    [Fact]
    public async Task Checkout_Writes_Domain_Events_To_Outbox()
    {
        ResetDatabase();
        var (client, _, productId) = await NewCustomerAsync();

        await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 2));

        await client.PostAsJsonAsync(
            "/api/checkout",
            new CheckoutRequest("123 Main St", null, "Mumbai", "Maharashtra", "400001", "India"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        var events = db.OutboxMessages
            .Select(o => o.EventType)
            .ToList();

        Assert.Contains("OrderCreatedEvent", events);
        Assert.Contains("OrderPlacedEvent", events);
    }

    [Fact]
    public async Task Outbox_Processing_Dispatches_Events_And_Sends_Confirmation_Email()
    {
        ResetDatabase();
        var (client, userId, productId) = await NewCustomerAsync();

        await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(productId, 2));

        await client.PostAsJsonAsync(
            "/api/checkout",
            new CheckoutRequest("123 Main St", null, "Mumbai", "Maharashtra", "400001", "India"));

        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider;

        var processor = provider.GetRequiredService<IOutboxProcessingService>();
        var result = await processor.ProcessPendingAsync(100, 3, CancellationToken.None);

        Assert.True(result.Total >= 2);
        Assert.Equal(0, result.Failed);
        Assert.True(result.Processed >= 2);

        var emailService = provider.GetRequiredService<IEmailService>();
        var mockEmail = Assert.IsType<MockEmailService>(emailService);
        var emails = mockEmail.GetSentEmails();

        Assert.NotEmpty(emails);
        Assert.Contains(userId, emails.Select(e => e.UserId).ToList());
    }
}
