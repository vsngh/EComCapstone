using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly ECommerceDbContext _dbContext;

    public InventoryRepository(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Inventory?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.Inventory
            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task AddAsync(Inventory inventory, CancellationToken cancellationToken)
    {
        await _dbContext.Inventory.AddAsync(inventory, cancellationToken);
    }

    public void Update(Inventory inventory)
    {
        _dbContext.Inventory.Update(inventory);
    }
}
