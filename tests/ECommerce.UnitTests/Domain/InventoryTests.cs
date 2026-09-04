using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;

namespace ECommerce.UnitTests.Domain;

public class InventoryTests
{
    private static readonly Guid ProductId = Guid.NewGuid();

    private static Inventory CreateInventory(int available = 100)
    {
        return new Inventory(ProductId, available);
    }

    [Fact]
    public void Create_With_Negative_Quantity_Throws()
    {
        Assert.Throws<DomainException>(() => new Inventory(ProductId, -1));
    }

    [Fact]
    public void Create_With_Empty_Product_Throws()
    {
        Assert.Throws<DomainException>(() => new Inventory(Guid.Empty, 10));
    }

    [Fact]
    public void AddStock_Increases_Available()
    {
        var inventory = CreateInventory(100);

        inventory.AddStock(10);

        Assert.Equal(110, inventory.AvailableQuantity);
        Assert.Equal(0, inventory.ReservedQuantity);
    }

    [Fact]
    public void AddStock_With_Non_Positive_Throws()
    {
        var inventory = CreateInventory(100);

        Assert.Throws<DomainException>(() => inventory.AddStock(0));
    }

    [Fact]
    public void Reserve_Moves_Stock_To_Reserved()
    {
        var inventory = CreateInventory(100);

        inventory.Reserve(5);

        Assert.Equal(95, inventory.AvailableQuantity);
        Assert.Equal(5, inventory.ReservedQuantity);
    }

    [Fact]
    public void Reserve_More_Than_Available_Throws()
    {
        var inventory = CreateInventory(2);

        Assert.Throws<InsufficientStockException>(() => inventory.Reserve(3));
    }

    [Fact]
    public void Release_Returns_Stock_To_Available()
    {
        var inventory = CreateInventory(100);
        inventory.Reserve(5);

        inventory.Release(5);

        Assert.Equal(100, inventory.AvailableQuantity);
        Assert.Equal(0, inventory.ReservedQuantity);
    }

    [Fact]
    public void Release_More_Than_Reserved_Throws()
    {
        var inventory = CreateInventory(100);
        inventory.Reserve(5);

        Assert.Throws<DomainException>(() => inventory.Release(6));
    }

    [Fact]
    public void Reserve_Never_Makes_Available_Negative()
    {
        var inventory = CreateInventory(10);

        inventory.Reserve(10);

        Assert.Equal(0, inventory.AvailableQuantity);
        Assert.Equal(10, inventory.ReservedQuantity);
    }

    [Fact]
    public void Full_Reserve_Release_Cycle_Restores_State()
    {
        var inventory = CreateInventory(10);

        inventory.Reserve(5);
        inventory.Reserve(3);
        inventory.Release(8);

        Assert.Equal(10, inventory.AvailableQuantity);
        Assert.Equal(0, inventory.ReservedQuantity);
    }
}
