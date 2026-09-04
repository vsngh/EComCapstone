using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events;

public record OrderCreatedEvent(
    Guid OrderId,
    Guid UserId,
    decimal TotalAmount) : DomainEvent;
