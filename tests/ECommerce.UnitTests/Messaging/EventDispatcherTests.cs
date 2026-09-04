using System.Text.Json;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Messaging;
using ECommerce.Application.Notifications;
using ECommerce.Domain.Events;

namespace ECommerce.UnitTests.Messaging;

public class EventDispatcherTests
{
    private sealed class FakeHandler : IEventHandler
    {
        public string EventType { get; }
        public int HandleCalls;
        public string? ReceivedPayload;

        public FakeHandler(string eventType)
        {
            EventType = eventType;
        }

        public Task HandleAsync(string payloadJson, CancellationToken cancellationToken)
        {
            HandleCalls++;
            ReceivedPayload = payloadJson;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Dispatch_Routes_To_Registered_Handler()
    {
        var handler = new FakeHandler("OrderPlacedEvent");
        var dispatcher = new EventDispatcher([handler]);

        await dispatcher.DispatchAsync("OrderPlacedEvent", "{}", CancellationToken.None);

        Assert.Equal(1, handler.HandleCalls);
        Assert.Equal("{}", handler.ReceivedPayload);
    }

    [Fact]
    public async Task Dispatch_Unknown_Type_Is_NoOp()
    {
        var handler = new FakeHandler("OrderPlacedEvent");
        var dispatcher = new EventDispatcher([handler]);

        await dispatcher.DispatchAsync("UnknownEvent", "{}", CancellationToken.None);

        Assert.Equal(0, handler.HandleCalls);
    }
}

public class OrderPlacedEventHandlerTests
{
    private sealed class FakeEmailService : IEmailService
    {
        public Guid? UserId;
        public Guid? OrderId;
        public decimal? TotalAmount;
        public int SendCalls;

        public Task SendOrderConfirmationAsync(Guid userId, Guid orderId, decimal totalAmount, CancellationToken cancellationToken)
        {
            SendCalls++;
            UserId = userId;
            OrderId = orderId;
            TotalAmount = totalAmount;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task HandleAsync_Sends_Email_With_Event_Data()
    {
        var emailService = new FakeEmailService();
        var orderPlaced = new OrderPlacedEvent(Guid.NewGuid(), Guid.NewGuid(), 42m);
        var payload = JsonSerializer.Serialize(orderPlaced, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var handler = new OrderPlacedEventHandler(emailService);

        await handler.HandleAsync(payload, CancellationToken.None);

        Assert.Equal(1, emailService.SendCalls);
        Assert.Equal(orderPlaced.UserId, emailService.UserId);
        Assert.Equal(orderPlaced.OrderId, emailService.OrderId);
        Assert.Equal(orderPlaced.TotalAmount, emailService.TotalAmount);
    }
}