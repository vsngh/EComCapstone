using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ClosedXML.Excel;
using ECommerce.Application.Products.DTOs;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public class ProductBulkUploadTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _adminClient = default!;

    public ProductBulkUploadTests()
    {
        _factory = TestDatabase.CreateFactory("BulkUpload");
    }

    public async Task InitializeAsync()
    {
        await TestDatabase.SeedAsync(_factory);
        var token = await AuthHelper.EnsureAdminTokenAsync(_factory);
        _adminClient = AuthHelper.CreateClientWithToken(_factory, token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task BulkUpload_Valid_Xlsx_Creates_Products_And_Inventory()
    {
        var excel = CreateWorkbookBytes(
            new[] { "Name", "SKU", "Price", "Category", "Description", "StockQuantity" },
            new[] { "Bulk Phone", "BULK-001", "15000", "Electronics", "Bulk phone", "25" },
            new[] { "Bulk Laptop", "BULK-002", "60000", "Electronics", "Bulk laptop", "5" });

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(excel);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "products.xlsx");

        var response = await _adminClient.PostAsync("/api/products/bulk-upload", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BulkUploadResult>();
        Assert.NotNull(result);
        Assert.Equal(2, result!.TotalRows);
        Assert.Equal(2, result.Imported);
        Assert.Equal(0, result.Failed);
        Assert.Empty(result.Errors);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        Assert.NotNull(db.Products.FirstOrDefault(p => p.Sku == "BULK-001"));
        Assert.NotNull(db.Products.FirstOrDefault(p => p.Sku == "BULK-002"));

        var phone = db.Products.First(p => p.Sku == "BULK-001");
        var inventory = db.Inventory.FirstOrDefault(i => i.ProductId == phone.Id);
        Assert.NotNull(inventory);
        Assert.Equal(25, inventory!.AvailableQuantity);
    }

    [Fact]
    public async Task BulkUpload_Reports_Row_Errors_Without_Importing_Bad_Rows()
    {
        var excel = CreateWorkbookBytes(
            new[] { "Name", "SKU", "Price", "Category", "Description", "StockQuantity" },
            new[] { "Good Phone", "BULK-G1", "15000", "Electronics", "Good", "10" },
            new[] { "Bad Price", "BULK-B1", "0", "Electronics", "Bad", "10" },
            new[] { "Unknown Cat", "BULK-B2", "100", "NoSuchCategory", "Bad", "10" },
            new[] { "Dup Sku", "BULK-G1", "100", "Electronics", "Dup", "10" });

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(excel);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "products.xlsx");

        var response = await _adminClient.PostAsync("/api/products/bulk-upload", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BulkUploadResult>();
        Assert.NotNull(result);
        Assert.Equal(4, result!.TotalRows);
        Assert.Equal(1, result.Imported);
        Assert.Equal(3, result.Failed);
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.Error.Contains("greater than zero"));
        Assert.Contains(result.Errors, e => e.Error.Contains("does not exist"));
        Assert.Contains(result.Errors, e => e.Error.Contains("appears more than once"));
    }

    [Fact]
    public async Task BulkUpload_Requires_Admin()
    {
        var anon = _factory.CreateClient();

        var excel = CreateWorkbookBytes(
            new[] { "Name", "SKU", "Price", "Category" },
            new[] { "X", "SKU-X", "10", "Electronics" });

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(excel);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "products.xlsx");

        var response = await anon.PostAsync("/api/products/bulk-upload", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BulkUpload_Invalid_Xlsx_Returns_BadRequest()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("this is not an xlsx"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "products.xlsx");

        var response = await _adminClient.PostAsync("/api/products/bulk-upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static byte[] CreateWorkbookBytes(string[] headers, params string?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        for (var r = 0; r < rows.Length; r++)
        {
            var row = rows[r];
            for (var c = 0; c < headers.Length; c++)
            {
                ws.Cell(r + 2, c + 1).Value = row[c] ?? string.Empty;
            }
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}