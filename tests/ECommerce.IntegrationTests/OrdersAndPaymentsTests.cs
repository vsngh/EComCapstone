using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.Carts.DTOs;
using ECommerce.Application.Orders.DTOs;
using ECommerce.Application.Payments.DTOs;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public class OrdersAndPaymentsTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrdersAndPaymentsTests()
    {
        _factory = TestDatabase.CreateFactory("Orders");
    }

    public async Task InitializeAsync()
    {
        await TestDatabase.SeedAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private Guid GetProductId(string? maxPrice = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        if (maxPrice == "expensive")
        {
            return db.Products.OrderByDescending(p => p.Price.Amount).First().Id;
        }

        return db.Products.First().Id;
    }

    private int GetAvailableQuantity(Guid productId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        return db.Inventory.First(i => i.ProductId == productId).AvailableQuantity;
    }

    private void ResetDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        db.Payments.RemoveRange(db.Payments);
        db.Orders.RemoveRange(db.Orders);
        db.CartItems.RemoveRange(db.CartItems);
        db.Carts.RemoveRange(db.Carts);

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

    private static CheckoutRequest Checkout(string? idempotencyKey = null) =>
        new("123 Main St", null, "Mumbai", "Maharashtra", "400001", "India",
            PaymentMethod.Card, idempotencyKey);

    private async Task<HttpClient> PopulateCartAsync(Guid productId, int qty)
    {
        var (client, _) = await AuthHelper.NewCustomerAsync(_factory);
        await client.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(productId, qty));
        return client;
    }

    [Fact]
    public async Task Checkout_Success_Confirms_Order_And_Payment()
    {
        ResetDatabase();
        var productId = GetProductId();
        var client = await PopulateCartAsync(productId, 2);

        var response = await client.PostAsJsonAsync("/api/checkout", Checkout());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Confirmed, order.Status);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        var payment = db.Payments.First(p => p.OrderId == order.Id);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
    }

    [Fact]
    public async Task Checkout_Declined_Payment_Fails_Order_And_Releases_Inventory()
    {
        ResetDatabase();
        var productId = GetProductId("expensive");
        var client = await PopulateCartAsync(productId, 30);

        var response = await client.PostAsJsonAsync("/api/checkout", Checkout());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        var order = db.Orders.First();
        Assert.Equal(OrderStatus.PaymentFailed, order.Status);

        var payment = db.Payments.First(p => p.OrderId == order.Id);
        Assert.Equal(PaymentStatus.Failed, payment.Status);

        Assert.Equal(100, GetAvailableQuantity(productId));
    }

    [Fact]
    public async Task Checkout_With_Same_Idempotency_Key_Is_Replay_Safe()
    {
        ResetDatabase();
        var productId = GetProductId();
        var client = await PopulateCartAsync(productId, 1);

        var first = await client.PostAsJsonAsync("/api/checkout", Checkout("key-abc-123"));
        var firstOrder = await first.Content.ReadFromJsonAsync<OrderResponse>();

        var second = await client.PostAsJsonAsync("/api/checkout", Checkout("key-abc-123"));
        var secondOrder = await second.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(firstOrder!.Id, secondOrder!.Id);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        Assert.Single(db.Orders);
        Assert.Single(db.Payments);
    }

    [Fact]
    public async Task User_Can_View_Own_Order_But_Not_Another_Users()
    {
        ResetDatabase();
        var productId = GetProductId();

        var owner = await PopulateCartAsync(productId, 1);
        var orderResp = await owner.PostAsJsonAsync("/api/checkout", Checkout());
        var order = await orderResp.Content.ReadFromJsonAsync<OrderResponse>();

        var (otherClient, _) = await AuthHelper.NewCustomerAsync(_factory);

        var ownerGet = await owner.GetAsync($"/api/orders/{order!.Id}");
        var otherGet = await otherClient.GetAsync($"/api/orders/{order!.Id}");

        Assert.Equal(HttpStatusCode.OK, ownerGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherGet.StatusCode);
    }

    [Fact]
    public async Task Cancel_Order_Releases_Inventory()
    {
        ResetDatabase();
        var productId = GetProductId();
        var client = await PopulateCartAsync(productId, 5);

        var orderResp = await client.PostAsJsonAsync("/api/checkout", Checkout());
        var order = await orderResp.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(95, GetAvailableQuantity(productId));

        var cancelResp = await client.PostAsJsonAsync(
            $"/api/orders/{order!.Id}/cancel",
            new CancelOrderRequest("Changed my mind"));

        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);

        var cancelled = await cancelResp.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
        Assert.Equal(100, GetAvailableQuantity(productId));
    }

    [Fact]
    public async Task Admin_Can_Update_Order_Status()
    {
        ResetDatabase();
        var productId = GetProductId();
        var client = await PopulateCartAsync(productId, 1);

        var orderResp = await client.PostAsJsonAsync("/api/checkout", Checkout());
        var order = await orderResp.Content.ReadFromJsonAsync<OrderResponse>();

        var token = await AuthHelper.EnsureAdminTokenAsync(_factory);
        var adminClient = AuthHelper.CreateClientWithToken(_factory, token);

        var resp = await adminClient.PutAsJsonAsync(
            $"/api/admin/orders/{order!.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Packed));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var updated = await resp.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(OrderStatus.Packed, updated!.Status);
    }

    [Fact]
    public async Task Admin_Can_List_All_Orders()
    {
        ResetDatabase();
        var productId = GetProductId();
        var client = await PopulateCartAsync(productId, 1);
        await client.PostAsJsonAsync("/api/checkout", Checkout());

        var token = await AuthHelper.EnsureAdminTokenAsync(_factory);
        var adminClient = AuthHelper.CreateClientWithToken(_factory, token);

        var resp = await adminClient.GetAsync("/api/admin/orders");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var orders = await resp.Content.ReadFromJsonAsync<List<OrderResponse>>();
        Assert.Single(orders!);
    }

    [Fact]
    public async Task Webhook_With_Unknown_Reference_Returns_BadRequest()
    {
        var client = _factory.CreateClient();

        var webhook = new PaymentWebhookRequest("payment.succeeded", "unknown-ref", true, null);
        var body = System.Text.Json.JsonSerializer.Serialize(webhook);
        var signature = ECommerce.Api.Security.WebhookSignatureVerifier.ComputeHmacSha256(
            body,
            "dev-webhook-secret-change-me");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/payments/webhook")
        {
            Content = new StringContent(
                body,
                System.Text.Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("X-Signature", signature);

        var resp = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}