using ECommerce.Domain.Common;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Entities;

public class Cart : Entity
{
    private readonly List<CartItem> _items = new();

    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public bool IsEmpty => _items.Count == 0;

    public int TotalItems => _items.Sum(i => i.Quantity);

    private Cart()
    {
    }

    public Cart(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Cart must belong to a valid user.");
        }

        UserId = userId;
    }

    public CartItem AddItem(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        product.EnsurePurchasable();

        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);

        if (existing is not null)
        {
            existing.UpdateQuantity(existing.Quantity + quantity);
            return existing;
        }

        var item = CartItem.Create(Id, product.Id, product.Price, quantity);
        _items.Add(item);
        return item;
    }

    public void UpdateItemQuantity(Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new DomainException("Item not found in cart.");

        item.UpdateQuantity(quantity);
    }

    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new DomainException("Item not found in cart.");

        _items.Remove(item);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
