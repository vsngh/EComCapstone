using ECommerce.Application.Payments.DTOs;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Payments;

public class MockPaymentProvider : IPaymentProvider
{
    private readonly ILogger<MockPaymentProvider> _logger;

    private const decimal DeclineThreshold = 1_000_000m;

    public MockPaymentProvider(ILogger<MockPaymentProvider> logger)
    {
        _logger = logger;
    }

    public Task<PaymentResult> AuthorizeAsync(
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount > DeclineThreshold)
        {
            _logger.LogWarning(
                "Mock payment declined for order {OrderId} of {Amount} {Currency}",
                request.OrderId,
                request.Amount,
                request.Currency);

            return Task.FromResult(new PaymentResult(
                false,
                null,
                "Card declined: amount exceeds authorization limit."));
        }

        var reference = $"mock-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Mock payment approved for order {OrderId} with reference {Reference}",
            request.OrderId,
            reference);

        return Task.FromResult(new PaymentResult(true, reference, null));
    }
}