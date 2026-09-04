using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IOrderNotifier _orderNotifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IOrderNotifier orderNotifier,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _orderNotifier = orderNotifier;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OrderResponse?> GetOrderAsync(
        Guid userId,
        Guid orderId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        if (!isAdmin && order.UserId != userId)
        {
            return null;
        }

        return MapToResponse(order);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetUserOrdersAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId, cancellationToken);
        return orders.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        return orders.Select(MapToResponse).ToList();
    }

    public async Task<OrderResponse> CancelAsync(
        Guid userId,
        Guid orderId,
        string? reason,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new Domain.Exceptions.OrderNotFoundException(orderId);

        if (!isAdmin && order.UserId != userId)
        {
            throw new Domain.Exceptions.OrderNotFoundException(orderId);
        }

        order.Cancel(reason);

        await ReleaseReservedStockAsync(order, cancellationToken);

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderNumber} cancelled", order.OrderNumber);

        await _orderNotifier.NotifyStatusChangedAsync(
            order.Id, order.UserId, order.Status, cancellationToken);

        return MapToResponse(order);
    }

    public async Task<OrderResponse> UpdateStatusAsync(
        Guid orderId,
        OrderStatus status,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new Domain.Exceptions.OrderNotFoundException(orderId);

        order.UpdateStatus(status);

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _orderNotifier.NotifyStatusChangedAsync(
            order.Id, order.UserId, order.Status, cancellationToken);

        return MapToResponse(order);
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