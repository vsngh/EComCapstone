namespace ECommerce.Application.Carts.DTOs;

public record AddCartItemRequest(
    Guid ProductId,
    int Quantity);

public record UpdateCartItemQuantityRequest(
    int Quantity);