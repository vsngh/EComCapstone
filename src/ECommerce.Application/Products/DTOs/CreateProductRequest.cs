namespace ECommerce.Application.Products.DTOs;

public record CreateProductRequest(
    string Name,
    string Sku,
    decimal Price,
    Guid CategoryId,
    string? Description);
