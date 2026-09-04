using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events;

public record PaymentFailedEvent(
    Guid PaymentId,
    Guid OrderId,
    string? FailureReason) : DomainEvent;
