using ECommerce.Domain.Common;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Entities;

public class Inventory : Entity
{
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public int AvailableQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public byte[] Version { get; private set; } = Array.Empty<byte>();

    public int TotalQuantity => AvailableQuantity + ReservedQuantity;

    private Inventory()
    {
    }

    public Inventory(Guid productId, int availableQuantity)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Inventory must belong to a valid product.");
        }

        if (availableQuantity < 0)
        {
            throw new DomainException("Available quantity cannot be negative.");
        }

        ProductId = productId;
        AvailableQuantity = availableQuantity;
        ReservedQuantity = 0;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity to add must be greater than zero.");
        }

        AvailableQuantity += quantity;
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity to reserve must be greater than zero.");
        }

        if (quantity > AvailableQuantity)
        {
            throw new InsufficientStockException(ProductId, quantity, AvailableQuantity);
        }

        AvailableQuantity -= quantity;
        ReservedQuantity += quantity;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity to release must be greater than zero.");
        }

        if (quantity > ReservedQuantity)
        {
            throw new DomainException(
                $"Cannot release {quantity} units; only {ReservedQuantity} units are reserved.");
        }

        ReservedQuantity -= quantity;
        AvailableQuantity += quantity;
    }

    public void SetAvailableQuantity(int quantity)
    {
        if (quantity < 0)
        {
            throw new DomainException("Available quantity cannot be negative.");
        }

        AvailableQuantity = quantity;
    }
}
