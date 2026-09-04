using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events;

public record PaymentCompletedEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount) : DomainEvent;
