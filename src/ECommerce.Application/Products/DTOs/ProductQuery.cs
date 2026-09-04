namespace ECommerce.Application.Products.DTOs;

public record ProductQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    string? SortBy = null,
    string SortDirection = "asc",
    bool? IsActive = null)
{
    public string CacheKey =>
        $"{Page}|{PageSize}|{Search ?? "":n}|{CategoryId ?? Guid.Empty:n}|{SortBy ?? "":n}|{SortDirection}|{IsActive}";
}
