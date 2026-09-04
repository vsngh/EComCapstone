using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Events;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.UnitTests.Domain;

public class OrderTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private static Order CreateOrder()
    {
        var order = Order.Create(
            UserId,
            "ORD-1001",
            new Money(200m),
            new AddressValue("Street", "City", "State", "560001"));

        order.AddItem(ProductId, "Laptop", "SKU-001", new Money(100m), 2);
        return order;
    }

    [Fact]
    public void Create_Emits_OrderCreated_Event_And_Is_Pending()
    {
        var order = CreateOrder();

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Contains(order.DomainEvents, e => e is OrderCreatedEvent);
    }

    [Fact]
    public void Create_With_Empty_User_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Order.Create(Guid.Empty, "ORD-1", new Money(100m), null));
    }

    [Fact]
    public void Create_With_Empty_OrderNumber_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Order.Create(UserId, " ", new Money(100m), null));
    }

    [Fact]
    public void Complete_Without_Items_Throws()
    {
        var order = Order.Create(UserId, "ORD-1", new Money(100m), null);

        Assert.Throws<InvalidOrderStateException>(order.Complete);
    }

    [Fact]
    public void Complete_Moves_To_Confirmed_And_Emits_Placed_Event()
    {
        var order = CreateOrder();

        order.Complete();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Contains(order.DomainEvents, e => e is OrderPlacedEvent);
    }

    [Fact]
    public void Delivered_Order_Cannot_Move_Back_To_Pending()
    {
        var order = CreateOrder();
        order.Complete();
        order.UpdateStatus(OrderStatus.Packed);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);

        Assert.Throws<InvalidOrderStateException>(() =>
            order.UpdateStatus(OrderStatus.Pending));
    }

    [Fact]
    public void Cancelled_Order_Cannot_Be_Confirmed()
    {
        var order = CreateOrder();
        order.Cancel();

        Assert.Throws<InvalidOrderStateException>(order.Complete);
    }

    [Fact]
    public void Invalid_Direct_Transition_Throws()
    {
        var order = CreateOrder();

        Assert.Throws<InvalidOrderStateException>(() =>
            order.UpdateStatus(OrderStatus.Shipped));
    }

    [Fact]
    public void Valid_Progression_Is_Allowed()
    {
        var order = CreateOrder();
        order.Complete();

        order.UpdateStatus(OrderStatus.Packed);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);

        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void MarkPaymentFailed_Transitions_To_PaymentFailed()
    {
        var order = CreateOrder();

        order.MarkPaymentFailed();

        Assert.Equal(OrderStatus.PaymentFailed, order.Status);
    }
}
