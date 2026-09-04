using ECommerce.Application.Inventory;
using ECommerce.Application.Inventory.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/products/{productId:guid}/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;

    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<ActionResult<InventoryResponse>> GetInventory(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.GetByProductIdAsync(productId, cancellationToken);
        return Ok(inventory);
    }

    [HttpPost("add")]
    public async Task<ActionResult<InventoryResponse>> AddStock(
        Guid productId,
        [FromBody] AddStockRequest request,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.AddStockAsync(productId, request.Quantity, cancellationToken);
        return Ok(inventory);
    }

    [HttpPost("reserve")]
    public async Task<ActionResult<InventoryResponse>> ReserveStock(
        Guid productId,
        [FromBody] ReserveStockRequest request,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.ReserveStockAsync(productId, request.Quantity, cancellationToken);
        return Ok(inventory);
    }

    [HttpPost("release")]
    public async Task<ActionResult<InventoryResponse>> ReleaseStock(
        Guid productId,
        [FromBody] ReleaseStockRequest request,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.ReleaseStockAsync(productId, request.Quantity, cancellationToken);
        return Ok(inventory);
    }
}