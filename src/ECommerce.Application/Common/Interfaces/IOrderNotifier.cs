using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Interfaces;

public interface IOrderNotifier
{
    Task NotifyStatusChangedAsync(
        Guid orderId,
        Guid userId,
        OrderStatus status,
        CancellationToken cancellationToken);
}