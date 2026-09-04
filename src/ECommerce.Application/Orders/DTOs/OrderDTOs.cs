using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders.DTOs;

public record CancelOrderRequest(string? Reason = null);

public record UpdateOrderStatusRequest(OrderStatus Status);