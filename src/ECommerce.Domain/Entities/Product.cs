using ECommerce.Domain.Common;
using ECommerce.Domain.Events;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class Product : Entity
{
    public string Name { get; private set; }
    public string Sku { get; private set; }
    public string? Description { get; private set; }
    public Money Price { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }

    private Product()
    {
        Name = string.Empty;
        Sku = string.Empty;
        Price = Money.Zero();
    }

    private Product(
        string name,
        string sku,
        Money price,
        Guid categoryId,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("SKU is required.");
        }

        if (price.Amount <= 0)
        {
            throw new DomainException("Product price must be greater than zero.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Product must belong to a valid category.");
        }

        Name = name;
        Sku = sku;
        Price = price;
        CategoryId = categoryId;
        Description = description;
        IsActive = true;

        AddDomainEvent(new ProductCreatedEvent(Id, Sku, Name));
    }

    public static Product Create(
        string name,
        string sku,
        Money price,
        Guid categoryId,
        string? description = null)
    {
        return new Product(name, sku, price, categoryId, description);
    }

    public void Update(
        string name,
        Money price,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name cannot be empty.");
        }

        if (price.Amount <= 0)
        {
            throw new DomainException("Product price must be greater than zero.");
        }

        Name = name;
        Price = price;
        Description = description;
    }

    public void ChangePrice(Money price)
    {
        if (price.Amount <= 0)
        {
            throw new DomainException("Product price must be greater than zero.");
        }

        Price = price;
    }

    public void ChangeCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Product must belong to a valid category.");
        }

        CategoryId = categoryId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void EnsurePurchasable()
    {
        if (!IsActive)
        {
            throw new DomainException($"Product '{Name}' is inactive and cannot be purchased.");
        }
    }
}
