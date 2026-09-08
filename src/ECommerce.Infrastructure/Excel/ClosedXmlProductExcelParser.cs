using System.Globalization;
using ClosedXML.Excel;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.DTOs;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Infrastructure.Excel;

public class ClosedXmlProductExcelParser : IProductExcelParser
{
    private const string HeaderName = "Name";
    private const string HeaderSku = "SKU";
    private const string HeaderPrice = "Price";
    private const string HeaderCategory = "Category";
    private const string HeaderDescription = "Description";
    private const string HeaderStock = "StockQuantity";

    private static readonly string[] RequiredHeaders =
        [HeaderName, HeaderSku, HeaderPrice, HeaderCategory];

    public ExcelParseResult Parse(Stream stream)
    {
        try
        {
            return ParseInternal(stream);
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new DomainException(
                "Could not read the uploaded file as a valid Excel (.xlsx) workbook.",
                ex);
        }
    }

    private static ExcelParseResult ParseInternal(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);

        if (!workbook.Worksheets.Any())
        {
            throw new DomainException("The uploaded Excel file contains no worksheets.");
        }

        var sheet = workbook.Worksheets.First();

        if (sheet.RowCount() < 2)
        {
            throw new DomainException("The uploaded Excel file contains no product rows.");
        }

        var headers = ReadHeaders(sheet);
        EnsureRequiredColumns(headers);

        var rows = new List<ProductImportRow>();
        var errors = new List<BulkUploadRowError>();
        var totalRows = 0;

        for (var rowNumber = 2; rowNumber <= sheet.LastRowUsed()?.RowNumber(); rowNumber++)
        {
            var row = sheet.Row(rowNumber);

            var name = GetCellValueOrDefault(row, headers, HeaderName);
            var sku = GetCellValueOrDefault(row, headers, HeaderSku);
            var priceRaw = GetCellValueOrDefault(row, headers, HeaderPrice);
            var category = GetCellValueOrDefault(row, headers, HeaderCategory);
            var description = GetCellValueOrDefault(row, headers, HeaderDescription);
            var stockRaw = GetCellValueOrDefault(row, headers, HeaderStock);

            if (string.IsNullOrWhiteSpace(name) &&
                string.IsNullOrWhiteSpace(sku) &&
                string.IsNullOrWhiteSpace(priceRaw) &&
                string.IsNullOrWhiteSpace(category))
            {
                continue;
            }

            totalRows++;

            if (!TryParsePrice(priceRaw, out var price))
            {
                errors.Add(new BulkUploadRowError(
                    rowNumber,
                    $"Price '{priceRaw}' is not a valid number."));
                continue;
            }

            if (price <= 0)
            {
                errors.Add(new BulkUploadRowError(
                    rowNumber,
                    "Price must be greater than zero."));
                continue;
            }

            var stockParse = TryParseStock(stockRaw);
            if (stockParse.Error is not null)
            {
                errors.Add(new BulkUploadRowError(rowNumber, stockParse.Error));
                continue;
            }

            rows.Add(new ProductImportRow(
                rowNumber,
                name.Trim(),
                sku.Trim(),
                price,
                category.Trim(),
                string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                stockParse.Value));
        }

        if (rows.Count == 0 && errors.Count == 0)
        {
            throw new DomainException("The uploaded Excel file contains no product rows.");
        }

        return new ExcelParseResult(totalRows, rows, errors);
    }

    private static Dictionary<string, int> ReadHeaders(IXLWorksheet sheet)
    {
        var headerRow = sheet.FirstRowUsed() ?? throw new DomainException(
            "The uploaded Excel file has no header row.");

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var value = cell.GetFormattedString().Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!headers.ContainsKey(value))
            {
                headers[value] = cell.Address.ColumnNumber;
            }
        }

        return headers;
    }

    private static void EnsureRequiredColumns(IReadOnlyDictionary<string, int> headers)
    {
        var missing = RequiredHeaders
            .Where(h => !headers.ContainsKey(h))
            .ToList();

        if (missing.Count > 0)
        {
            throw new DomainException(
                $"The Excel file is missing required columns: {string.Join(", ", missing)}. " +
                $"Expected columns: {string.Join(", ", RequiredHeaders)}.");
        }
    }

    private static string GetCellValueOrDefault(
        IXLRow row,
        IReadOnlyDictionary<string, int> headers,
        string header)
    {
        if (!headers.TryGetValue(header, out var column))
        {
            return string.Empty;
        }

        return row.Cell(column).GetFormattedString().Trim();
    }

    private static bool TryParsePrice(string value, out decimal price)
    {
        value = value.Replace(",", string.Empty);

        return decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out price);
    }

    private static (int? Value, string? Error) TryParseStock(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (null, null);
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock))
        {
            return (null, $"StockQuantity '{value}' is not a valid integer.");
        }

        if (stock < 0)
        {
            return (null, "StockQuantity cannot be negative.");
        }

        return (stock, null);
    }
}