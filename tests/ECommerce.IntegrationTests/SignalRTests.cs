using System.Net;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public class SignalRTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public SignalRTests()
    {
        _factory = TestDatabase.CreateFactory("SignalR");
    }

    public async Task InitializeAsync()
    {
        await TestDatabase.SeedAsync(_factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task OrderHub_Route_Is_Mapped_Not_404()
    {
        var client = _factory.CreateClient();

        // The hub route must exist (not 404). SignalR hubs require authentication,
        // so an anonymous request will be challenged rather than missing.
        var response = await client.GetAsync("/hubs/orders");

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OrderNotifier_Notifies_Without_Error()
    {
        using var scope = _factory.Services.CreateScope();
        var notifier = scope.ServiceProvider.GetRequiredService<IOrderNotifier>();

        await notifier.NotifyStatusChangedAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrderStatus.Shipped,
            CancellationToken.None);
    }
}