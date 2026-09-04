using ECommerce.Domain.Common;

namespace ECommerce.Application.Common.Interfaces;

public interface IOutboxMessageRepository
{
    Task AddAsync(DomainEvent domainEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken);
    Task MarkProcessedAsync(OutboxMessage message, CancellationToken cancellationToken);
    Task IncrementRetryCountAsync(OutboxMessage message, string? error, CancellationToken cancellationToken);
}
