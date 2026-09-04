using System.Text.Json;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Outbox;

public class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly ECommerceDbContext _dbContext;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OutboxMessageRepository(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);

        var message = new OutboxMessage(domainEvent.GetType().Name, payload);

        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken)
    {
        return await _dbContext.OutboxMessages
            .Where(o => o.ProcessedAt == null)
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        message.MarkProcessed(DateTime.UtcNow);
        _dbContext.OutboxMessages.Update(message);
        await Task.CompletedTask;
    }

    public async Task IncrementRetryCountAsync(OutboxMessage message, string? error, CancellationToken cancellationToken)
    {
        message.RegisterFailure(error);
        _dbContext.OutboxMessages.Update(message);
        await Task.CompletedTask;
    }
}
