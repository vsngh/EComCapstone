using ECommerce.Domain.Common;

namespace ECommerce.Domain.Exceptions;

public class InventoryNotFoundException : DomainException
{
    public InventoryNotFoundException(Guid productId)
        : base($"Inventory for product '{productId}' was not found.")
    {
    }
}
