using System.Text.Json;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Messaging;
using ECommerce.Domain.Events;

namespace ECommerce.Application.Notifications;

public class OrderPlacedEventHandler : IEventHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IEmailService _emailService;

    public OrderPlacedEventHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public string EventType => "OrderPlacedEvent";

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var orderPlaced = JsonSerializer.Deserialize<OrderPlacedEvent>(payloadJson, JsonOptions);
        if (orderPlaced == null)
        {
            return;
        }

        await _emailService.SendOrderConfirmationAsync(
            orderPlaced.UserId,
            orderPlaced.OrderId,
            orderPlaced.TotalAmount,
            cancellationToken);
    }
}