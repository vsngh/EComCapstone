using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Events;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Entities;

public class Payment : Entity
{
    public Guid OrderId { get; private set; }
    public Order? Order { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? IdempotencyKey { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }

    private Payment()
    {
    }

    private Payment(
        Guid orderId,
        decimal amount,
        PaymentMethod method,
        string? idempotencyKey)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Payment must belong to a valid order.");
        }

        if (amount <= 0)
        {
            throw new DomainException("Payment amount must be positive.");
        }

        OrderId = orderId;
        Amount = amount;
        Method = method;
        IdempotencyKey = idempotencyKey;
        Status = PaymentStatus.Pending;
    }

    public static Payment Create(
        Guid orderId,
        decimal amount,
        PaymentMethod method,
        string? idempotencyKey = null)
    {
        return new Payment(orderId, amount, method, idempotencyKey);
    }

    public void MarkProcessing()
    {
        ValidateTransition(PaymentStatus.Processing);
        Status = PaymentStatus.Processing;
    }

    public void Complete(string providerReference)
    {
        ValidateTransition(PaymentStatus.Succeeded);
        Status = PaymentStatus.Succeeded;
        ProviderReference = providerReference;
        CompletedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentCompletedEvent(Id, OrderId, Amount));
    }

    public void Fail(string? failureReason)
    {
        ValidateTransition(PaymentStatus.Failed);
        Status = PaymentStatus.Failed;
        FailureReason = failureReason;
        CompletedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentFailedEvent(Id, OrderId, failureReason));
    }

    public void Refund()
    {
        if (Status != PaymentStatus.Succeeded)
        {
            throw new PaymentException("Only successful payments can be refunded.");
        }

        Status = PaymentStatus.Refunded;
    }

    private void ValidateTransition(PaymentStatus newStatus)
    {
        if (newStatus == Status)
        {
            return;
        }

        var valid = (Status, newStatus) switch
        {
            (PaymentStatus.Pending, PaymentStatus.Processing) => true,
            (PaymentStatus.Pending, PaymentStatus.Failed) => true,
            (PaymentStatus.Processing, PaymentStatus.Succeeded) => true,
            (PaymentStatus.Processing, PaymentStatus.Failed) => true,
            _ => false
        };

        if (!valid)
        {
            throw new PaymentException(
                $"Payment cannot transition from '{Status}' to '{newStatus}'.");
        }
    }
}
