using ECommerce.Domain.Entities;
using InventoryEntity = ECommerce.Domain.Entities.Inventory;

namespace ECommerce.Application.Common.Interfaces;

public interface IInventoryRepository
{
    Task<InventoryEntity?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);
    Task AddAsync(InventoryEntity inventory, CancellationToken cancellationToken);
    void Update(InventoryEntity inventory);
}