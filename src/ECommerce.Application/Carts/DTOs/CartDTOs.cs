namespace ECommerce.Application.Carts.DTOs;

public record CartItemResponse(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public record CartResponse(
    Guid CartId,
    Guid UserId,
    int TotalItems,
    decimal TotalAmount,
    IReadOnlyList<CartItemResponse> Items);