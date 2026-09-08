namespace ECommerce.Application.Products.DTOs;

public sealed record ProductImportRow(
    int RowNumber,
    string Name,
    string Sku,
    decimal Price,
    string CategoryName,
    string? Description,
    int? StockQuantity);

public sealed record BulkUploadRowError(
    int RowNumber,
    string Error);

public sealed record ExcelParseResult(
    int TotalRows,
    IReadOnlyList<ProductImportRow> Rows,
    IReadOnlyList<BulkUploadRowError> Errors);

public sealed record BulkUploadResult(
    int TotalRows,
    int Imported,
    int Failed,
    IReadOnlyList<BulkUploadRowError> Errors);