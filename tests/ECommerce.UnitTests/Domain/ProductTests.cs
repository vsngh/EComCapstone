using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Events;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.UnitTests.Domain;

public class ProductTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static Product CreateProduct(decimal price = 100m, bool active = true)
    {
        var product = Product.Create(
            "Laptop",
            "SKU-001",
            new Money(price),
            CategoryId,
            "A laptop");

        if (!active)
        {
            product.Deactivate();
        }

        return product;
    }

    [Fact]
    public void Create_With_Empty_Name_Throws()
    {
        var action = () => Product.Create(
            " ",
            "SKU-001",
            new Money(100m),
            CategoryId);

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Create_With_Empty_Sku_Throws()
    {
        var action = () => Product.Create(
            "Laptop",
            " ",
            new Money(100m),
            CategoryId);

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Create_With_Non_Positive_Price_Throws()
    {
        var action = () => Product.Create(
            "Laptop",
            "SKU-001",
            new Money(0m),
            CategoryId);

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Create_With_Empty_Category_Throws()
    {
        var action = () => Product.Create(
            "Laptop",
            "SKU-001",
            new Money(100m),
            Guid.Empty);

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Create_With_Valid_Data_Succeeds_And_Emits_Event()
    {
        var product = CreateProduct();

        Assert.Equal("Laptop", product.Name);
        Assert.Equal("SKU-001", product.Sku);
        Assert.Equal(100m, product.Price.Amount);
        Assert.True(product.IsActive);
        Assert.Contains(product.DomainEvents, e => e is ProductCreatedEvent);
    }

    [Fact]
    public void ChangePrice_With_Non_Positive_Price_Throws()
    {
        var product = CreateProduct();

        Assert.Throws<DomainException>(() => product.ChangePrice(new Money(-5m)));
    }

    [Fact]
    public void ChangePrice_With_Valid_Price_Succeeds()
    {
        var product = CreateProduct();

        product.ChangePrice(new Money(150m));

        Assert.Equal(150m, product.Price.Amount);
    }

    [Fact]
    public void Deactivate_Prevents_Purchase()
    {
        var product = CreateProduct(active: false);

        Assert.False(product.IsActive);
        Assert.Throws<DomainException>(product.EnsurePurchasable);
    }

    [Fact]
    public void EnsurePurchasable_Succeeds_For_Active_Product()
    {
        var product = CreateProduct(active: true);

        product.EnsurePurchasable();
    }
}
