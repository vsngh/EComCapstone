namespace ECommerce.Application.Products.DTOs;

public record UpdateProductRequest(
    string Name,
    decimal Price,
    string? Description);
