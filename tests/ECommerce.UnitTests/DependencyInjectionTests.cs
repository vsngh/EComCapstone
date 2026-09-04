using ECommerce.Application;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_Should_Not_Throw()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider);
    }
}
