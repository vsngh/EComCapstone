using ECommerce.Application.Common.Cache;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products;
using ECommerce.Application.Products.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using InventoryEntity = ECommerce.Domain.Entities.Inventory;

namespace ECommerce.UnitTests;

public class BulkProductImportServiceTests
{
    private readonly FakeProductRepository _productRepo = new();
    private readonly FakeCategoryRepository _categoryRepo = new();
    private readonly FakeInventoryRepository _inventoryRepo = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeCacheService _cache = new();
    private readonly BulkProductImportService _service;

    public BulkProductImportServiceTests()
    {
        _service = new BulkProductImportService(
            new FakeExcelParser(),
            _productRepo,
            _categoryRepo,
            _inventoryRepo,
            _uow,
            _cache,
            NullLogger<BulkProductImportService>.Instance);
    }

    [Fact]
    public async Task Import_Valid_Rows_Creates_Products_And_Inventory()
    {
        var result = await _service.ImportAsync(Stream.Null, CancellationToken.None);

        Assert.Equal(2, result.Imported);
        Assert.Equal(0, result.Failed);
        Assert.Equal(2, _productRepo.Added.Count);
        Assert.Equal(2, _inventoryRepo.Added.Count);
        Assert.True(_uow.Saved);
    }

    [Fact]
    public async Task Import_Duplicate_Sku_In_File_Reports_Error()
    {
        var service = new BulkProductImportService(
            new FakeExcelParser(includeDuplicateSku: true),
            _productRepo,
            _categoryRepo,
            _inventoryRepo,
            _uow,
            _cache,
            NullLogger<BulkProductImportService>.Instance);

        var result = await service.ImportAsync(Stream.Null, CancellationToken.None);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Errors, e => e.Error.Contains("appears more than once"));
    }

    [Fact]
    public async Task Import_Missing_Category_Reports_Error()
    {
        var service = new BulkProductImportService(
            new FakeExcelParser(includeUnknownCategory: true),
            _productRepo,
            _categoryRepo,
            _inventoryRepo,
            _uow,
            _cache,
            NullLogger<BulkProductImportService>.Instance);

        var result = await service.ImportAsync(Stream.Null, CancellationToken.None);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Errors, e => e.Error.Contains("does not exist"));
    }

    [Fact]
    public async Task Import_Empty_File_Throws_DomainException()
    {
        var service = new BulkProductImportService(
            new FakeExcelParser(emptyResult: true),
            _productRepo,
            _categoryRepo,
            _inventoryRepo,
            _uow,
            _cache,
            NullLogger<BulkProductImportService>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.ImportAsync(Stream.Null, CancellationToken.None));
    }

    [Fact]
    public async Task Import_Invalid_Price_Reports_Error()
    {
        var service = new BulkProductImportService(
            new FakeExcelParser(includeInvalidPrice: true),
            _productRepo,
            _categoryRepo,
            _inventoryRepo,
            _uow,
            _cache,
            NullLogger<BulkProductImportService>.Instance);

        var result = await service.ImportAsync(Stream.Null, CancellationToken.None);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Errors, e => e.Error.Contains("greater than zero"));
    }

    [Fact]
    public async Task Import_Clears_Product_Cache_On_Success()
    {
        await _service.ImportAsync(Stream.Null, CancellationToken.None);

        Assert.True(_cache.PrefixDeleted);
    }

    [Fact]
    public async Task Import_Negative_Stock_Reports_Error()
    {
        var service = new BulkProductImportService(
            new FakeExcelParser(includeNegativeStock: true),
            _productRepo,
            _categoryRepo,
            _inventoryRepo,
            _uow,
            _cache,
            NullLogger<BulkProductImportService>.Instance);

        var result = await service.ImportAsync(Stream.Null, CancellationToken.None);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Errors, e => e.Error.Contains("cannot be negative"));
    }

    private static Category CreateCategory(string name)
    {
        return new Category(name, null);
    }

    private class FakeExcelParser : IProductExcelParser
    {
        private readonly bool _emptyResult;
        private readonly bool _includeDuplicateSku;
        private readonly bool _includeUnknownCategory;
        private readonly bool _includeInvalidPrice;
        private readonly bool _includeNegativeStock;

        public FakeExcelParser(
            bool emptyResult = false,
            bool includeDuplicateSku = false,
            bool includeUnknownCategory = false,
            bool includeInvalidPrice = false,
            bool includeNegativeStock = false)
        {
            _emptyResult = emptyResult;
            _includeDuplicateSku = includeDuplicateSku;
            _includeUnknownCategory = includeUnknownCategory;
            _includeInvalidPrice = includeInvalidPrice;
            _includeNegativeStock = includeNegativeStock;
        }

        public ExcelParseResult Parse(Stream stream)
        {
            if (_emptyResult)
            {
                return new ExcelParseResult(0, [], []);
            }

            var rows = new List<ProductImportRow>
            {
                new(2, "Product A", "SKU-A", 100m, "Electronics", "Desc A", 10),
                new(3, "Product B", "SKU-B", 200m, "Books", "Desc B", 0)
            };

            var errors = new List<BulkUploadRowError>();

            if (_includeDuplicateSku)
            {
                rows.Add(new ProductImportRow(4, "Product C", "SKU-A", 300m, "Electronics", null, 5));
            }

            if (_includeUnknownCategory)
            {
                rows.Add(new ProductImportRow(4, "Product C", "SKU-C", 300m, "Unknown Cat", null, 5));
            }

            if (_includeInvalidPrice)
            {
                errors.Add(new BulkUploadRowError(4, "Price must be greater than zero."));
            }

            if (_includeNegativeStock)
            {
                errors.Add(new BulkUploadRowError(4, "StockQuantity cannot be negative."));
            }

            var totalRows = rows.Count + errors.Count;

            return new ExcelParseResult(totalRows, rows, errors);
        }
    }

    private class FakeProductRepository : IProductRepository
    {
        public List<Product> Added { get; } = [];

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult<Product?>(null);

        public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct)
            => Task.FromResult<Product?>(null);

        public Task<bool> SkuExistsAsync(string sku, CancellationToken ct)
            => Task.FromResult(false);

        public Task AddAsync(Product product, CancellationToken ct)
        {
            Added.Add(product);
            return Task.CompletedTask;
        }

        public void Update(Product product) { }

        public void Delete(Product product) { }

        public Task<ProductListResponse> GetProductsAsync(
            Application.Products.DTOs.ProductQuery query, CancellationToken ct)
        {
            return Task.FromResult(
                new ProductListResponse([], 1, 20, 0, 0));
        }
    }

    private class FakeCategoryRepository : ICategoryRepository
    {
        public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult<Category?>(null);

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
            => Task.FromResult(false);

        public Task AddAsync(Category category, CancellationToken ct)
            => Task.CompletedTask;

        public void Update(Category category) { }

        public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct)
        {
            IReadOnlyList<Category> cats =
            [
                CreateCategory("Electronics"),
                CreateCategory("Books"),
                CreateCategory("Fashion")
            ];
            return Task.FromResult(cats);
        }
    }

    private class FakeInventoryRepository : IInventoryRepository
    {
        public List<InventoryEntity> Added { get; } = [];

        public Task<InventoryEntity?> GetByProductIdAsync(
            Guid productId, CancellationToken ct)
            => Task.FromResult<InventoryEntity?>(null);

        public Task AddAsync(InventoryEntity inventory, CancellationToken ct)
        {
            Added.Add(inventory);
            return Task.CompletedTask;
        }

        public void Update(InventoryEntity inventory) { }
    }

    private class FakeUnitOfWork : IUnitOfWork
    {
        public bool Saved { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken ct)
        {
            Saved = true;
            return Task.FromResult(1);
        }
    }

    private class FakeCacheService : ICacheService
    {
        public bool PrefixDeleted { get; private set; }

        public Task<T?> GetAsync<T>(string key, CancellationToken ct)
            where T : class => Task.FromResult<T?>(null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
            where T : class => Task.CompletedTask;

        public Task DeleteAsync(string key, CancellationToken ct) => Task.CompletedTask;

        public Task DeleteByPrefixAsync(string prefix, CancellationToken ct)
        {
            PrefixDeleted = true;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct)
            => Task.FromResult(false);

        public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;
    }
}