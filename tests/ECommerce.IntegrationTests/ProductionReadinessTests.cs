using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ECommerce.Api.Security;
using ECommerce.Application.Payments.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.IntegrationTests;

public class ProductionReadinessTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductionReadinessTests()
    {
        _factory = TestDatabase.CreateFactory("ProdReadiness");
    }

    public async Task InitializeAsync()
    {
        await TestDatabase.SeedAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_Liveness_Endpoint_Returns_Ok()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_Ready_Endpoint_Returns_Ok()
    {
        var response = await _factory.CreateClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Metrics_Endpoint_Requires_Admin()
    {
        var anonymous = await _factory.CreateClient().GetAsync("/api/admin/metrics");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var token = await AuthHelper.EnsureAdminTokenAsync(_factory);
        var adminClient = AuthHelper.CreateClientWithToken(_factory, token);

        var authorized = await adminClient.GetAsync("/api/admin/metrics");
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
    }

    [Fact]
    public async Task Payment_Webhook_Rejects_Missing_Signature()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/payments/webhook",
            new PaymentWebhookRequest("payment.completed", "ref-123", true, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Payment_Webhook_Rejects_Invalid_Signature()
    {
        var webhook = new PaymentWebhookRequest("payment.completed", "ref-123", true, null);
        var request = CreateWebhookRequest(webhook, "garbage-signature-value");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Payment_Webhook_With_Valid_Signature_Is_Not_Rejected_For_Authorization()
    {
        var webhook = new PaymentWebhookRequest("payment.completed", "ref-123", true, null);

        // The server verifies the HMAC over the exact serialized request body,
        // so compute the signature over the same serialization it will reproduce.
        var body = JsonSerializer.Serialize(webhook);
        var signature = WebhookSignatureVerifier.ComputeHmacSha256(
            body,
            "dev-webhook-secret-change-me");

        var request = CreateWebhookRequest(webhook, signature);

        var response = await _factory.CreateClient().SendAsync(request);

        // The signature passes; the webhook itself may then fail on an unknown
        // reference, but it must not be rejected due to an invalid signature.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpRequestMessage CreateWebhookRequest(PaymentWebhookRequest webhook, string signature)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/payments/webhook")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(webhook),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("X-Signature", signature);
        return request;
    }
}