using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.UnitTests.Domain;

public class CartTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static Cart CreateCart()
    {
        return new Cart(UserId);
    }

    private static Product CreateProduct(decimal price = 100m, bool active = true)
    {
        var product = Product.Create("Laptop", "SKU-001", new Money(price), CategoryId);

        if (!active)
        {
            product.Deactivate();
        }

        return product;
    }

    [Fact]
    public void Create_With_Empty_User_Throws()
    {
        Assert.Throws<DomainException>(() => new Cart(Guid.Empty));
    }

    [Fact]
    public void Initially_Empty()
    {
        var cart = CreateCart();

        Assert.True(cart.IsEmpty);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void AddItem_With_Non_Positive_Quantity_Throws()
    {
        var cart = CreateCart();
        var product = CreateProduct();

        Assert.Throws<DomainException>(() => cart.AddItem(product, 0));
    }

    [Fact]
    public void AddItem_With_Inactive_Product_Throws()
    {
        var cart = CreateCart();
        var product = CreateProduct(active: false);

        Assert.Throws<DomainException>(() => cart.AddItem(product, 1));
    }

    [Fact]
    public void AddItem_Uses_Server_Price()
    {
        var cart = CreateCart();
        var product = CreateProduct(price: 250m);

        cart.AddItem(product, 1);

        Assert.Equal(250m, cart.Items.Single().UnitPrice.Amount);
    }

    [Fact]
    public void AddItem_Adds_Item_To_Cart()
    {
        var cart = CreateCart();
        var product = CreateProduct();

        cart.AddItem(product, 2);

        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items.Single().Quantity);
    }

    [Fact]
    public void AddItem_Same_Product_Increments_Quantity()
    {
        var cart = CreateCart();
        var product = CreateProduct();

        cart.AddItem(product, 1);
        cart.AddItem(product, 2);

        Assert.Single(cart.Items);
        Assert.Equal(3, cart.Items.Single().Quantity);
    }

    [Fact]
    public void RemoveItem_Removes_Target_Product()
    {
        var cart = CreateCart();
        var product = CreateProduct();
        cart.AddItem(product, 1);

        cart.RemoveItem(product.Id);

        Assert.Empty(cart.Items);
    }

    [Fact]
    public void RemoveItem_Non_Existent_Throws()
    {
        var cart = CreateCart();

        Assert.Throws<DomainException>(() => cart.RemoveItem(Guid.NewGuid()));
    }

    [Fact]
    public void Clear_Removes_All_Items()
    {
        var cart = CreateCart();
        cart.AddItem(CreateProduct(price: 100m), 1);
        cart.AddItem(CreateProduct(price: 200m), 2);

        cart.Clear();

        Assert.True(cart.IsEmpty);
    }

    [Fact]
    public void UpdateItemQuantity_Updates_Correctly()
    {
        var cart = CreateCart();
        var product = CreateProduct();
        cart.AddItem(product, 1);

        cart.UpdateItemQuantity(product.Id, 5);

        Assert.Equal(5, cart.Items.Single().Quantity);
    }
}
