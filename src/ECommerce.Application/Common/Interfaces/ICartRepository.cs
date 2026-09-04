using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(Cart cart, CancellationToken cancellationToken);
    void Update(Cart cart);
}
