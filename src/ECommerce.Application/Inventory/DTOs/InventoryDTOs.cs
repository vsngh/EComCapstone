namespace ECommerce.Application.Inventory.DTOs;

public record InventoryResponse(
    Guid ProductId,
    string? ProductName,
    int AvailableQuantity,
    int ReservedQuantity,
    int TotalQuantity);

public record AddStockRequest(
    int Quantity);

public record ReserveStockRequest(
    int Quantity);

public record ReleaseStockRequest(
    int Quantity);