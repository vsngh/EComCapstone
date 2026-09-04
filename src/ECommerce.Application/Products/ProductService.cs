using ECommerce.Application.Common.Cache;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Products;

public class ProductService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private const string ListCacheKey = "products:list";
    private const string DetailCachePrefix = "products:detail:";

    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ProductListResponse> GetProductsAsync(
        ProductQuery query,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{ListCacheKey}:{query.CacheKey}";

        var cached = await _cacheService.GetAsync<ProductListResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var result = await _productRepository.GetProductsAsync(query, cancellationToken);

        await _cacheService.SetAsync(cacheKey, result, CacheTtl, cancellationToken);

        return result;
    }

    public async Task<ProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{DetailCachePrefix}{id}";

        var cached = await _cacheService.GetAsync<ProductResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var response = MapToResponse(product);
        await _cacheService.SetAsync(cacheKey, response, CacheTtl, cancellationToken);

        return response;
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        if (await _productRepository.SkuExistsAsync(request.Sku, cancellationToken))
        {
            throw new DomainException($"A product with SKU '{request.Sku}' already exists.");
        }

        if (!await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken))
        {
            throw new DomainException("Category does not exist.");
        }

        var product = Product.Create(
            request.Name,
            request.Sku,
            new Money(request.Price),
            request.CategoryId,
            request.Description);

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product {ProductId} with SKU {Sku} created",
            product.Id,
            product.Sku);

        var saved = await _productRepository.GetByIdAsync(product.Id, cancellationToken)
            ?? product;

        await InvalidateProductCacheAsync(cancellationToken);
        await _cacheService.DeleteAsync($"{DetailCachePrefix}{product.Id}", cancellationToken);

        return MapToResponse(saved);
    }

    public async Task<ProductResponse> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ProductNotFoundException(id);

        product.Update(request.Name, new Money(request.Price), request.Description);

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product {ProductId} updated", product.Id);

        await InvalidateProductCacheAsync(cancellationToken);
        await _cacheService.DeleteAsync($"{DetailCachePrefix}{id}", cancellationToken);

        return MapToResponse(product);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ProductNotFoundException(id);

        _productRepository.Delete(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product {ProductId} deleted", product.Id);

        await InvalidateProductCacheAsync(cancellationToken);
        await _cacheService.DeleteAsync($"{DetailCachePrefix}{id}", cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ProductNotFoundException(id);

        product.Deactivate();
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product {ProductId} deactivated", product.Id);

        await InvalidateProductCacheAsync(cancellationToken);
        await _cacheService.DeleteAsync($"{DetailCachePrefix}{id}", cancellationToken);
    }

    private async Task InvalidateProductCacheAsync(CancellationToken cancellationToken)
    {
        await _cacheService.DeleteByPrefixAsync(ListCacheKey, cancellationToken);
    }

    private static ProductResponse MapToResponse(Product product)
    {
        var categoryName = product.Category?.Name ?? string.Empty;

        return new ProductResponse(
            product.Id,
            product.Name,
            product.Sku,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.IsActive,
            product.CategoryId,
            categoryName);
    }
}
