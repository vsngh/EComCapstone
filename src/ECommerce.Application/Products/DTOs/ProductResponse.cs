namespace ECommerce.Application.Products.DTOs;

public record ProductResponse(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    string Currency,
    bool IsActive,
    Guid CategoryId,
    string CategoryName);
