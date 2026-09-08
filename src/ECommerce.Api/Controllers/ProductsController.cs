using ECommerce.Application.Products;
using ECommerce.Application.Products.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly BulkProductImportService _bulkImportService;

    public ProductsController(
        ProductService productService,
        BulkProductImportService bulkImportService)
    {
        _productService = productService;
        _bulkImportService = bulkImportService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ProductListResponse>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? category = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDirection = "asc",
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ProductQuery(
            page,
            pageSize,
            search,
            category,
            sortBy,
            sortDirection,
            isActive);

        var result = await _productService.GetProductsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductResponse>> GetProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.UpdateAsync(id, request, cancellationToken);
        return Ok(product);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _productService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("bulk-upload")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BulkUploadResult>> BulkUpload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await _bulkImportService.ImportAsync(stream, cancellationToken);
        return Ok(result);
    }
}
