using System.Text.Json;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ECommerceDbContext _dbContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public UnitOfWork(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        CaptureDomainEventsToOutbox();

        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void CaptureDomainEventsToOutbox()
    {
        var trackedEntities = _dbContext.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in trackedEntities)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                var payload = JsonSerializer.Serialize(
                    domainEvent,
                    domainEvent.GetType(),
                    JsonOptions);

                _dbContext.OutboxMessages.Add(
                    new OutboxMessage(domainEvent.GetType().Name, payload));
            }

            entity.ClearDomainEvents();
        }
    }
}
