using ECommerce.Application.Products.DTOs;
using ECommerce.Application.Products.Validators;
using FluentValidation.TestHelper;

namespace ECommerce.UnitTests;

public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    [Theory]
    [InlineData("", "SKU-1", 10, "1c9a667f-0000-0000-0000-000000000000")]
    [InlineData("Widget", "", 10, "1c9a667f-0000-0000-0000-000000000000")]
    [InlineData("Widget", "SKU-1", 0, "1c9a667f-0000-0000-0000-000000000000")]
    [InlineData("Widget", "SKU-1", -5, "1c9a667f-0000-0000-0000-000000000000")]
    [InlineData("Widget", "SKU-1", 10, "00000000-0000-0000-0000-000000000000")]
    public void Validate_Invalid_Returns_Error(
        string name,
        string sku,
        decimal price,
        string categoryId)
    {
        var request = new CreateProductRequest(
            name,
            sku,
            price,
            Guid.Parse(categoryId),
            null);

        var result = _validator.TestValidate(request);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Validate_Valid_Request_No_Errors()
    {
        var request = new CreateProductRequest(
            "Widget",
            "SKU-123",
            15.99m,
            Guid.NewGuid(),
            "A widget");

        var result = _validator.TestValidate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
