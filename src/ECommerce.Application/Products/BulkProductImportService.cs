using ECommerce.Application.Common.Cache;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Products;

public class BulkProductImportService
{
    private const string ListCacheKey = "products:list";

    private readonly IProductExcelParser _excelParser;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<BulkProductImportService> _logger;

    public BulkProductImportService(
        IProductExcelParser excelParser,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<BulkProductImportService> logger)
    {
        _excelParser = excelParser;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<BulkUploadResult> ImportAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var parseResult = _excelParser.Parse(stream);
        var rows = parseResult.Rows;
        var errors = parseResult.Errors.ToList();
        var totalRows = parseResult.TotalRows;

        if (rows.Count == 0 && errors.Count == 0)
        {
            throw new DomainException("The uploaded Excel file contains no product rows.");
        }

        var categories = await LoadCategoriesByNameAsync(cancellationToken);
        var existingSkus = await LoadExistingSkusAsync(rows, cancellationToken);
        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var createdProducts = new List<Product>();
        var createdInventory = new List<Domain.Entities.Inventory>();

        foreach (var row in rows)
        {
            var error = ValidateRow(row, categories, existingSkus, seenInFile);
            if (error is not null)
            {
                errors.Add(new BulkUploadRowError(row.RowNumber, error));
                continue;
            }

            var category = categories[row.CategoryName!];
            var product = Product.Create(
                row.Name!,
                row.Sku!,
                new Money(row.Price),
                category.Id,
                row.Description);

            createdProducts.Add(product);

            var stock = row.StockQuantity ?? 0;
            createdInventory.Add(new Domain.Entities.Inventory(product.Id, stock));

            seenInFile.Add(row.Sku!);
        }

        if (createdProducts.Count > 0)
        {
            foreach (var product in createdProducts)
            {
                await _productRepository.AddAsync(product, cancellationToken);
            }

            foreach (var inventory in createdInventory)
            {
                await _inventoryRepository.AddAsync(inventory, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Bulk product import completed: {Imported} created, {Failed} rows failed",
                createdProducts.Count,
                errors.Count);

            await _cacheService.DeleteByPrefixAsync(ListCacheKey, cancellationToken);
        }
        else
        {
            _logger.LogWarning(
                "Bulk product import finished with no valid rows: {Failed} failed",
                errors.Count);
        }

        return new BulkUploadResult(
            totalRows,
            createdProducts.Count,
            errors.Count,
            errors);
    }

    private async Task<Dictionary<string, Category>> LoadCategoriesByNameAsync(
        CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);

        return categories.ToDictionary(
            c => c.Name,
            c => c,
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> LoadExistingSkusAsync(
        IReadOnlyList<ProductImportRow> rows,
        CancellationToken cancellationToken)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sku in rows.Where(r => !string.IsNullOrWhiteSpace(r.Sku)).Select(r => r.Sku!))
        {
            if (!existing.Contains(sku) &&
                await _productRepository.SkuExistsAsync(sku, cancellationToken))
            {
                existing.Add(sku);
            }
        }

        return existing;
    }

    private static string? ValidateRow(
        ProductImportRow row,
        Dictionary<string, Category> categories,
        HashSet<string> existingSkus,
        HashSet<string> seenInFile)
    {
        if (string.IsNullOrWhiteSpace(row.Name))
        {
            return "Product name is required.";
        }

        if (row.Name.Trim().Length > 200)
        {
            return "Product name must be 200 characters or fewer.";
        }

        if (string.IsNullOrWhiteSpace(row.Sku))
        {
            return "SKU is required.";
        }

        if (row.Sku.Trim().Length > 64)
        {
            return "SKU must be 64 characters or fewer.";
        }

        if (existingSkus.Contains(row.Sku))
        {
            return $"A product with SKU '{row.Sku}' already exists.";
        }

        if (!seenInFile.Add(row.Sku))
        {
            return $"SKU '{row.Sku}' appears more than once in the file.";
        }

        if (row.Price <= 0)
        {
            return "Price must be greater than zero.";
        }

        if (string.IsNullOrWhiteSpace(row.CategoryName))
        {
            return "Category is required.";
        }

        if (!categories.ContainsKey(row.CategoryName))
        {
            return $"Category '{row.CategoryName}' does not exist.";
        }

        if (row.StockQuantity is < 0)
        {
            return "Stock quantity cannot be negative.";
        }

        if (row.Description?.Length > 2000)
        {
            return "Description must be 2000 characters or fewer.";
        }

        return null;
    }
}