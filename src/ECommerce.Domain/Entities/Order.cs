using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Events;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class Order : Entity
{
    private readonly List<OrderItem> _items = new();

    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public string OrderNumber { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public Money TotalAmount { get; private set; }
    public AddressValue? ShippingAddress { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public Guid? PaymentId { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order()
    {
        OrderNumber = string.Empty;
        TotalAmount = Money.Zero();
    }

    private Order(
        Guid userId,
        string orderNumber,
        Money totalAmount,
        AddressValue? shippingAddress)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Order must belong to a valid user.");
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainException("Order number is required.");
        }

        UserId = userId;
        OrderNumber = orderNumber;
        TotalAmount = totalAmount;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;

        AddDomainEvent(new OrderCreatedEvent(Id, UserId, TotalAmount.Amount));
    }

    public static Order Create(
        Guid userId,
        string orderNumber,
        Money totalAmount,
        AddressValue? shippingAddress)
    {
        return new Order(userId, orderNumber, totalAmount, shippingAddress);
    }

    public void AddItem(
        Guid productId,
        string productName,
        string sku,
        Money unitPrice,
        int quantity)
    {
        var item = OrderItem.Create(Id, productId, productName, sku, unitPrice, quantity);
        _items.Add(item);
    }

    public void Complete()
    {
        if (_items.Count == 0)
        {
            throw new InvalidOrderStateException("Order must contain at least one item before it can be confirmed.");
        }

        ValidateTransition(OrderStatus.Confirmed);
        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderPlacedEvent(Id, UserId, TotalAmount.Amount));
    }

    public void MarkPaymentProcessing()
    {
        ValidateTransition(OrderStatus.PaymentProcessing);
        Status = OrderStatus.PaymentProcessing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaymentFailed()
    {
        ValidateTransition(OrderStatus.PaymentFailed);
        Status = OrderStatus.PaymentFailed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        ValidateTransition(OrderStatus.Cancelled);
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        ValidateTransition(newStatus);
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignPayment(Guid paymentId)
    {
        if (paymentId == Guid.Empty)
        {
            throw new DomainException("Payment id must be valid.");
        }

        PaymentId = paymentId;
    }

    private void ValidateTransition(OrderStatus newStatus)
    {
        if (newStatus == Status)
        {
            return;
        }

        if (!IsValidTransition(Status, newStatus))
        {
            throw new InvalidOrderStateException(
                $"Order cannot transition from '{Status}' to '{newStatus}'.");
        }
    }

    private static bool IsValidTransition(OrderStatus current, OrderStatus next)
    {
        if (current == OrderStatus.Delivered)
        {
            return false;
        }

        if (current == OrderStatus.Cancelled)
        {
            return false;
        }

        if (current == OrderStatus.PaymentFailed)
        {
            return false;
        }

        return next switch
        {
            OrderStatus.PaymentProcessing => current == OrderStatus.Pending,
            OrderStatus.Confirmed => current is OrderStatus.Pending
                or OrderStatus.PaymentProcessing,
            OrderStatus.Packed => current is OrderStatus.Confirmed,
            OrderStatus.Shipped => current == OrderStatus.Packed,
            OrderStatus.Delivered => current == OrderStatus.Shipped,
            _ => true
        };
    }
}
