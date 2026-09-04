using ECommerce.Domain.Common;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Order? Order { get; private set; }
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public string ProductName { get; private set; }
    public string Sku { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    private OrderItem()
    {
        ProductName = string.Empty;
        Sku = string.Empty;
        UnitPrice = Money.Zero();
    }

    private OrderItem(
        Guid orderId,
        Guid productId,
        string productName,
        string sku,
        Money unitPrice,
        int quantity)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order item must belong to a valid order.");
        }

        if (productId == Guid.Empty)
        {
            throw new DomainException("Order item must reference a valid product.");
        }

        if (unitPrice.Amount < 0)
        {
            throw new DomainException("Order item unit price cannot be negative.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Order item quantity must be greater than zero.");
        }

        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public static OrderItem Create(
        Guid orderId,
        Guid productId,
        string productName,
        string sku,
        Money unitPrice,
        int quantity)
    {
        return new OrderItem(orderId, productId, productName, sku, unitPrice, quantity);
    }

    public Money GetLineTotal()
    {
        return UnitPrice.Multiply(Quantity);
    }
}
