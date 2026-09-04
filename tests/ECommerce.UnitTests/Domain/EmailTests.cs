using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.UnitTests.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_With_Empty_Value_Throws(string? value)
    {
        var action = () => new Email(value!);

        Assert.ThrowsAny<Exception>(action);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    [InlineData("@domain.com")]
    [InlineData("user domain.com")]
    public void Create_With_Invalid_Format_Throws(string value)
    {
        Assert.Throws<DomainException>(() => new Email(value));
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last@example.co.in")]
    public void Create_With_Valid_Format_Succeeds(string value)
    {
        var email = new Email(value);

        Assert.Equal(value, email.Value);
    }

    [Fact]
    public void ToString_Returns_Value()
    {
        var email = new Email("test@example.com");

        Assert.Equal("test@example.com", email.ToString());
    }

    [Fact]
    public void Email_Equality_Is_Case_Sensitive_Value_Equality()
    {
        var first = new Email("test@example.com");
        var second = new Email("test@example.com");

        Assert.Equal(first, second);
    }
}
