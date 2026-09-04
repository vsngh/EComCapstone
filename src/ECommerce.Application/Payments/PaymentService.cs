using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Payments.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Payments;

public class PaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPaymentProvider paymentProvider,
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _paymentProvider = paymentProvider;
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProcessPaymentOutcome> ProcessOrderPaymentAsync(
        Order order,
        decimal amount,
        string currency,
        PaymentMethod method,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _paymentRepository.GetByIdempotencyKeyAsync(
                idempotencyKey, cancellationToken);

            if (existing is not null)
            {
                _logger.LogInformation(
                    "Reusing existing payment {PaymentId} for idempotency key {Key}",
                    existing.Id,
                    idempotencyKey);

                return new ProcessPaymentOutcome(
                    existing,
                    existing.Status == PaymentStatus.Succeeded,
                    existing.FailureReason,
                    WasReplayed: true);
            }
        }

        var payment = Payment.Create(order.Id, amount, method, idempotencyKey);
        payment.MarkProcessing();

        order.MarkPaymentProcessing();
        order.AssignPayment(payment.Id);

        await _paymentRepository.AddAsync(payment, cancellationToken);

        var result = await _paymentProvider.AuthorizeAsync(
            new PaymentRequest(order.Id, amount, currency, method, idempotencyKey),
            cancellationToken);

        if (result.Succeeded)
        {
            payment.Complete(result.ProviderReference!);
            order.Complete();
        }
        else
        {
            payment.Fail(result.FailureReason);
            order.MarkPaymentFailed();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProcessPaymentOutcome(payment, result.Succeeded, result.FailureReason);
    }

    public async Task<PaymentResponse> HandleWebhookAsync(
        PaymentWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByProviderReferenceAsync(
            request.ProviderReference, cancellationToken)
            ?? throw new PaymentException(
                $"No payment found for provider reference '{request.ProviderReference}'.");

        if (payment.Status is PaymentStatus.Succeeded
            or PaymentStatus.Failed
            or PaymentStatus.Refunded)
        {
            return MapToResponse(payment);
        }

        var order = await _orderRepository.GetByIdAsync(payment.OrderId, cancellationToken);

        if (request.Succeeded)
        {
            payment.Complete(request.ProviderReference);
            order?.Complete();
        }
        else
        {
            payment.Fail(request.FailureReason);
            order?.MarkPaymentFailed();

            if (order is not null)
            {
                await ReleaseReservedStockAsync(order, cancellationToken);
            }
        }

        _paymentRepository.Update(payment);
        if (order is not null)
        {
            _orderRepository.Update(order);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(payment);
    }

    private async Task ReleaseReservedStockAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            var inventory = await _inventoryRepository.GetByProductIdAsync(
                item.ProductId, cancellationToken);

            if (inventory is not null)
            {
                inventory.Release(item.Quantity);
                _inventoryRepository.Update(inventory);
            }
        }
    }

    private static PaymentResponse MapToResponse(Payment payment)
    {
        return new PaymentResponse(
            payment.Id,
            payment.OrderId,
            payment.Amount,
            "INR",
            payment.Status.ToString(),
            payment.ProviderReference,
            payment.FailureReason,
            payment.CreatedAt);
    }

    public sealed record ProcessPaymentOutcome(
        Payment Payment,
        bool Succeeded,
        string? FailureReason,
        bool WasReplayed = false);
}