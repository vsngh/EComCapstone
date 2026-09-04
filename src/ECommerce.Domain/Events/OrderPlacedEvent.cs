using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events;

public record OrderPlacedEvent(
    Guid OrderId,
    Guid UserId,
    decimal TotalAmount) : DomainEvent;
