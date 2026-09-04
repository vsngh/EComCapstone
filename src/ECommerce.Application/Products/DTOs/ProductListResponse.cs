namespace ECommerce.Application.Products.DTOs;

public record ProductListResponse(
    IReadOnlyList<ProductResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
