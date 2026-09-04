using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.UnitTests.Domain;

public class MoneyTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_With_Negative_Amount_Throws(decimal amount)
    {
        var action = () => new Money(amount);

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Create_With_Zero_Amount_Is_Allowed()
    {
        var money = new Money(0);

        Assert.Equal(0, money.Amount);
    }

    [Fact]
    public void Create_With_Valid_Amount_Succeeds()
    {
        var money = new Money(1250.50m);

        Assert.Equal(1250.50m, money.Amount);
        Assert.Equal("INR", money.Currency);
    }

    [Fact]
    public void Multiply_By_Quantity_Returns_Correct_Total()
    {
        var money = new Money(10m);

        var result = money.Multiply(3);

        Assert.Equal(30m, result.Amount);
    }

    [Fact]
    public void Add_Returns_Sum_Of_Same_Currency()
    {
        var first = new Money(10m);
        var second = new Money(5m);

        var result = first.Add(second);

        Assert.Equal(15m, result.Amount);
    }

    [Fact]
    public void Add_With_Different_Currency_Throws()
    {
        var inr = new Money(10m, "INR");
        var usd = new Money(10m, "USD");

        Assert.Throws<DomainException>(() => inr.Add(usd));
    }

    [Fact]
    public void Money_Equality_Is_Based_On_Value()
    {
        var first = new Money(99.99m);
        var second = new Money(99.99m);

        Assert.Equal(first, second);
        Assert.True(first == second);
    }
}
