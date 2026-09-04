using ECommerce.Application.Payments.DTOs;

namespace ECommerce.Application.Payments;

public interface IPaymentProvider
{
    Task<PaymentResult> AuthorizeAsync(PaymentRequest request, CancellationToken cancellationToken);
}