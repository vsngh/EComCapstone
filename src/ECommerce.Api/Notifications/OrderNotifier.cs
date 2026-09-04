using ECommerce.Api.Hubs;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.Api.Notifications;

public class OrderNotifier : IOrderNotifier
{
    private readonly IHubContext<OrderHub, IOrderNotificationClient> _hubContext;

    public OrderNotifier(IHubContext<OrderHub, IOrderNotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyStatusChangedAsync(
        Guid orderId,
        Guid userId,
        OrderStatus status,
        CancellationToken cancellationToken)
    {
        var message = new
        {
            orderId,
            status = status.ToString()
        };

        await _hubContext.Clients
            .Group($"user-{userId}")
            .ReceiveOrderStatusChanged(message.orderId, message.status);

        await _hubContext.Clients
            .Group($"order-{orderId}")
            .ReceiveOrderStatusChanged(message.orderId, message.status);
    }
}