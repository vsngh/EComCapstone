using ECommerce.Domain.Common;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class CartItem : Entity
{
    public Guid CartId { get; private set; }
    public Cart? Cart { get; private set; }
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    private CartItem()
    {
        UnitPrice = Money.Zero();
    }

    private CartItem(Guid cartId, Guid productId, Money unitPrice, int quantity)
    {
        if (cartId == Guid.Empty)
        {
            throw new DomainException("Cart item must belong to a valid cart.");
        }

        if (productId == Guid.Empty)
        {
            throw new DomainException("Cart item must reference a valid product.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Cart item quantity must be greater than zero.");
        }

        CartId = cartId;
        ProductId = productId;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public static CartItem Create(
        Guid cartId,
        Guid productId,
        Money unitPrice,
        int quantity)
    {
        return new CartItem(cartId, productId, unitPrice, quantity);
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Cart item quantity must be greater than zero.");
        }

        Quantity = quantity;
    }

    public Money GetLineTotal()
    {
        return UnitPrice.Multiply(Quantity);
    }
}
