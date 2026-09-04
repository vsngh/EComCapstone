using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Events;

public record OrderStatusChangedEvent(
    Guid OrderId,
    Guid UserId,
    OrderStatus OldStatus,
    OrderStatus NewStatus) : DomainEvent;
