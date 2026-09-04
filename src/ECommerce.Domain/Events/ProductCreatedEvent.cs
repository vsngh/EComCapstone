using ECommerce.Domain.Common;

namespace ECommerce.Domain.Events;

public record ProductCreatedEvent(
    Guid ProductId,
    string Sku,
    string Name) : DomainEvent;
