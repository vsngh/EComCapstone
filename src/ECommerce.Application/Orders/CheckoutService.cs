using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.DTOs;
using ECommerce.Application.Payments;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders;

public class CheckoutService
{
    private readonly ICartRepository _cartRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly PaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(
        ICartRepository cartRepository,
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IPaymentRepository paymentRepository,
        PaymentService paymentService,
        IUnitOfWork unitOfWork,
        ILogger<CheckoutService> logger)
    {
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _paymentRepository = paymentRepository;
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OrderResponse> CheckoutAsync(
        Guid userId,
        CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingPayment = await _paymentRepository.GetByIdempotencyKeyAsync(
                request.IdempotencyKey, cancellationToken);

            if (existingPayment is not null)
            {
                var existingOrder = await _orderRepository.GetByIdAsync(
                    existingPayment.OrderId, cancellationToken);

                if (existingOrder is not null && existingOrder.UserId == userId)
                {
                    return MapToResponse(existingOrder);
                }
            }
        }

        var cart = await _cartRepository.GetActiveByUserIdAsync(userId, cancellationToken)
            ?? throw new CartNotFoundException(userId);

        if (cart.IsEmpty)
        {
            throw new DomainException("Cannot checkout an empty cart.");
        }

        var shippingAddress = BuildAddress(request);

        var totalAmount = cart.Items.Sum(i => i.GetLineTotal().Amount);

        var orderNumber = await GenerateOrderNumberAsync(cancellationToken);
        var order = Order.Create(userId, orderNumber, new Money(totalAmount), shippingAddress);

        var reserved = new List<(Guid ProductId, int Quantity)>();

        foreach (var cartItem in cart.Items)
        {
            var inventory = await _inventoryRepository.GetByProductIdAsync(cartItem.ProductId, cancellationToken)
                ?? throw new InsufficientStockException(cartItem.ProductId, cartItem.Quantity, 0);

            inventory.Reserve(cartItem.Quantity);
            reserved.Add((cartItem.ProductId, cartItem.Quantity));

            order.AddItem(
                cartItem.ProductId,
                cartItem.Product?.Name ?? "Unknown",
                cartItem.Product?.Sku ?? string.Empty,
                cartItem.UnitPrice,
                cartItem.Quantity);
        }

        await _orderRepository.AddAsync(order, cancellationToken);

        cart.Deactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var outcome = await _paymentService.ProcessOrderPaymentAsync(
            order,
            totalAmount,
            "INR",
            request.PaymentMethod,
            request.IdempotencyKey,
            cancellationToken);

        if (!outcome.Succeeded)
        {
            await ReleaseReservedStockAsync(reserved, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment failed for order {OrderNumber}: {FailureReason}",
                order.OrderNumber,
                outcome.FailureReason);

            throw new PaymentException(outcome.FailureReason ?? "Payment was declined.");
        }

        _logger.LogInformation(
            "Order {OrderNumber} placed for user {UserId} with {ItemCount} items, total {Total}",
            orderNumber,
            userId,
            order.Items.Count,
            totalAmount);

        return MapToResponse(order);
    }

    private async Task ReleaseReservedStockAsync(
        List<(Guid ProductId, int Quantity)> reserved,
        CancellationToken cancellationToken)
    {
        foreach (var (productId, quantity) in reserved)
        {
            var inventory = await _inventoryRepository.GetByProductIdAsync(productId, cancellationToken);
            if (inventory is not null)
            {
                inventory.Release(quantity);
                _inventoryRepository.Update(inventory);
            }
        }
    }

    private static AddressValue? BuildAddress(CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShippingAddressLine1))
        {
            return null;
        }

        return new AddressValue(
            request.ShippingAddressLine1,
            request.ShippingCity ?? "Unknown",
            request.ShippingState ?? "Unknown",
            request.ShippingPostalCode ?? "000000",
            request.ShippingCountry ?? "India");
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");

        var sequence = await _orderRepository.CountTodayAsync(dateStr, cancellationToken);

        return $"ORD-{dateStr}-{(sequence + 1):D4}";
    }

    private static OrderResponse MapToResponse(Order order)
    {
        var items = order.Items
            .Select(i => new OrderItemResponse(
                i.ProductId,
                i.ProductName,
                i.Sku,
                i.UnitPrice.Amount,
                i.Quantity,
                i.GetLineTotal().Amount))
            .ToList();

        return new OrderResponse(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.ShippingAddress,
            order.CreatedAt,
            items);
    }
}