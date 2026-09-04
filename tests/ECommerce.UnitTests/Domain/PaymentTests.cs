using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Events;
using ECommerce.Domain.Exceptions;

namespace ECommerce.UnitTests.Domain;

public class PaymentTests
{
    private static readonly Guid OrderId = Guid.NewGuid();

    private static Payment CreatePayment(decimal amount = 100m)
    {
        return Payment.Create(OrderId, amount, PaymentMethod.Card, "idem-001");
    }

    [Fact]
    public void Create_With_Non_Positive_Amount_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Payment.Create(OrderId, 0, PaymentMethod.Card));
    }

    [Fact]
    public void Create_With_Empty_Order_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Payment.Create(Guid.Empty, 100m, PaymentMethod.Card));
    }

    [Fact]
    public void Complete_After_Processing_Is_Allowed()
    {
        var payment = CreatePayment();
        payment.MarkProcessing();

        payment.Complete("REF-001");

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("REF-001", payment.ProviderReference);
        Assert.Contains(payment.DomainEvents, e => e is PaymentCompletedEvent);
    }

    [Fact]
    public void Complete_Without_Processing_Throws()
    {
        var payment = CreatePayment();

        Assert.Throws<PaymentException>(() => payment.Complete("REF-001"));
    }

    [Fact]
    public void Fail_After_Processing_Is_Allowed()
    {
        var payment = CreatePayment();
        payment.MarkProcessing();

        payment.Fail("Card declined");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Card declined", payment.FailureReason);
        Assert.Contains(payment.DomainEvents, e => e is PaymentFailedEvent);
    }

    [Fact]
    public void Refund_Non_Succeeded_Payment_Throws()
    {
        var payment = CreatePayment();

        Assert.Throws<PaymentException>(payment.Refund);
    }

    [Fact]
    public void Refund_Succeeded_Payment_Succeeds()
    {
        var payment = CreatePayment();
        payment.MarkProcessing();
        payment.Complete("REF-001");

        payment.Refund();

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    [Fact]
    public void Second_State_Mutation_After_Succeeded_Throws()
    {
        var payment = CreatePayment();
        payment.MarkProcessing();
        payment.Complete("REF-001");

        Assert.Throws<PaymentException>(() => payment.Fail("unexpected"));
    }
}
