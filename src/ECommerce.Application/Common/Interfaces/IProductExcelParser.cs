using ECommerce.Application.Products.DTOs;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductExcelParser
{
    ExcelParseResult Parse(Stream stream);
}