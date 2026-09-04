using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Inventory.DTOs;
using ECommerce.Domain.Exceptions;
using InventoryEntity = ECommerce.Domain.Entities.Inventory;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Inventory;

public class InventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<InventoryService> logger)
    {
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<InventoryResponse> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId, cancellationToken)
            ?? throw new InventoryNotFoundException(productId);

        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);

        return new InventoryResponse(
            inventory.ProductId,
            product?.Name,
            inventory.AvailableQuantity,
            inventory.ReservedQuantity,
            inventory.TotalQuantity);
    }

    public async Task<InventoryResponse> AddStockAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        var inventory = await GetOrCreateInventoryAsync(productId, cancellationToken);

        inventory.AddStock(quantity);

        _inventoryRepository.Update(inventory);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Added {Quantity} units to inventory for product {ProductId}",
            quantity,
            productId);

        return MapToResponse(inventory);
    }

    public async Task<InventoryResponse> ReserveStockAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId, cancellationToken)
            ?? throw new InventoryNotFoundException(productId);

        inventory.Reserve(quantity);

        _inventoryRepository.Update(inventory);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reserved {Quantity} units for product {ProductId}",
            quantity,
            productId);

        return MapToResponse(inventory);
    }

    public async Task<InventoryResponse> ReleaseStockAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId, cancellationToken)
            ?? throw new InventoryNotFoundException(productId);

        inventory.Release(quantity);

        _inventoryRepository.Update(inventory);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Released {Quantity} units for product {ProductId}",
            quantity,
            productId);

        return MapToResponse(inventory);
    }

    private async Task<InventoryEntity> GetOrCreateInventoryAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByProductIdAsync(productId, cancellationToken);

        if (inventory is not null)
        {
            return inventory;
        }

        if (await _productRepository.GetByIdAsync(productId, cancellationToken) is null)
        {
            throw new ProductNotFoundException(productId);
        }

        inventory = new InventoryEntity(productId, 0);
        await _inventoryRepository.AddAsync(inventory, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return inventory;
    }

    private static InventoryResponse MapToResponse(InventoryEntity inventory)
    {
        return new InventoryResponse(
            inventory.ProductId,
            null,
            inventory.AvailableQuantity,
            inventory.ReservedQuantity,
            inventory.TotalQuantity);
    }
}