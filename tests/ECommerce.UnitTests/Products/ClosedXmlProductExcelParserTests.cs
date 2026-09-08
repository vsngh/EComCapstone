using ClosedXML.Excel;
using ECommerce.Domain.Exceptions;
using ECommerce.Infrastructure.Excel;

namespace ECommerce.UnitTests;

public class ClosedXmlProductExcelParserTests
{
    private readonly ClosedXmlProductExcelParser _parser = new();

    [Fact]
    public void Parse_Valid_Workbook_Returns_Rows()
    {
        using var stream = CreateWorkbook(
            new[] { "Name", "SKU", "Price", "Category", "Description", "StockQuantity" },
            new[] { "Smartphone", "SKU-001", "15000", "Electronics", "A phone", "50" },
            new[] { "Laptop", "SKU-002", "60000", "Electronics", "Fast", "10" });

        var result = _parser.Parse(stream);
        var rows = result.Rows;

        Assert.Equal(2, rows.Count);
        Assert.Equal("Smartphone", rows[0].Name);
        Assert.Equal("SKU-001", rows[0].Sku);
        Assert.Equal(15000m, rows[0].Price);
        Assert.Equal("Electronics", rows[0].CategoryName);
        Assert.Equal("A phone", rows[0].Description);
        Assert.Equal(50, rows[0].StockQuantity);

        Assert.Equal("Laptop", rows[1].Name);
        Assert.Equal(60000m, rows[1].Price);
        Assert.Equal(10, rows[1].StockQuantity);
    }

    [Fact]
    public void Parse_Empty_Rows_Throws_DomainException()
    {
        using var stream = CreateWorkbook(new[] { "Name", "SKU", "Price", "Category" });

        var ex = Assert.Throws<DomainException>(() => _parser.Parse(stream));
        Assert.Contains("no product rows", ex.Message);
    }

    [Fact]
    public void Parse_Fully_Empty_Blank_Rows_Are_Skipped()
    {
        using var stream = CreateWorkbook(
            new[] { "Name", "SKU", "Price", "Category" },
            new[] { "Widget", "SKU-W1", "10", "Books" },
            new string?[] { null, null, null, null },
            new[] { "Gadget", "SKU-G1", "20", "Books" });

        var result = _parser.Parse(stream);
        var rows = result.Rows;

        Assert.Equal(2, rows.Count);
        Assert.Equal("Widget", rows[0].Name);
        Assert.Equal("Gadget", rows[1].Name);
    }

    [Fact]
    public void Parse_Missing_Required_Column_Throws_DomainException()
    {
        using var stream = CreateWorkbook(new[] { "Name", "SKU" });

        var ex = Assert.Throws<DomainException>(() => _parser.Parse(stream));
        Assert.Contains("Price", ex.Message);
        Assert.Contains("Category", ex.Message);
    }

    [Fact]
    public void Parse_Invalid_Price_Records_Row_Error()
    {
        using var stream = CreateWorkbook(
            new[] { "Name", "SKU", "Price", "Category" },
            new[] { "Test", "SKU-T1", "not-a-number", "Books" });

        var result = _parser.Parse(stream);

        Assert.Empty(result.Rows);
        Assert.Single(result.Errors);
        Assert.Contains("not a valid number", result.Errors[0].Error);
        Assert.Equal(1, result.TotalRows);
    }

    [Fact]
    public void Parse_Price_Below_Zero_Records_Row_Error()
    {
        using var stream = CreateWorkbook(
            new[] { "Name", "SKU", "Price", "Category" },
            new[] { "Test", "SKU-T1", "-5", "Books" });

        var result = _parser.Parse(stream);

        Assert.Empty(result.Rows);
        Assert.Single(result.Errors);
        Assert.Contains("greater than zero", result.Errors[0].Error);
    }

    [Fact]
    public void Parse_Invalid_Stock_Records_Row_Error()
    {
        using var stream = CreateWorkbook(
            new[] { "Name", "SKU", "Price", "Category", "StockQuantity" },
            new[] { "Test", "SKU-T1", "10", "Books", "abc" });

        var result = _parser.Parse(stream);

        Assert.Empty(result.Rows);
        Assert.Single(result.Errors);
        Assert.Contains("not a valid integer", result.Errors[0].Error);
    }

    [Fact]
    public void Parse_Negative_Stock_Records_Row_Error()
    {
        using var stream = CreateWorkbook(
            new[] { "Name", "SKU", "Price", "Category", "StockQuantity" },
            new[] { "Test", "SKU-T1", "10", "Books", "-3" });

        var result = _parser.Parse(stream);

        Assert.Empty(result.Rows);
        Assert.Single(result.Errors);
        Assert.Contains("cannot be negative", result.Errors[0].Error);
    }

    [Fact]
    public void Parse_Empty_Stock_Row_Number_Is_Correct()
    {
        using var stream = CreateWorkbook(
            new[] { "Name", "SKU", "Price", "Category" },
            new[] { "Row1", "SKU-1", "5", "Books" },
            new string?[] { null, null, null, null });

        var result = _parser.Parse(stream);
        var rows = result.Rows;

        Assert.Single(rows);
        Assert.Null(rows[0].StockQuantity);
    }

    [Fact]
    public void Parse_Corrupted_Stream_Throws_DomainException()
    {
        var buffer = "this is not an xlsx"u8.ToArray();
        using var stream = new MemoryStream(buffer);

        var ex = Assert.Throws<DomainException>(() => _parser.Parse(stream));
        Assert.Contains("valid Excel", ex.Message);
    }

    [Fact]
    public void Parse_Price_With_Commas_Is_Parsed_Correctly()
    {
        using var stream = CreateWorkbook(
            new[] { "Name", "SKU", "Price", "Category" },
            new[] { "Item", "SKU-I1", "15,000", "Electronics" });

        var result = _parser.Parse(stream);
        var rows = result.Rows;

        Assert.Single(rows);
        Assert.Equal(15000m, rows[0].Price);
    }

    private static MemoryStream CreateWorkbook(string[] headers, params string?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        for (var r = 0; r < rows.Length; r++)
        {
            var row = r + 2;
            for (var c = 0; c < headers.Length; c++)
            {
                ws.Cell(row, c + 1).Value = rows[r][c] ?? string.Empty;
            }
        }

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}