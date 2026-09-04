using ECommerce.Infrastructure.Caching;

namespace ECommerce.UnitTests.Caching;

public class InMemoryCacheServiceTests
{
    [Fact]
    public async Task Set_And_Get_Returns_Value()
    {
        var cache = new InMemoryCacheService();

        await cache.SetAsync("k", "val", TimeSpan.FromMinutes(1), CancellationToken.None);

        var value = await cache.GetAsync<string>("k", CancellationToken.None);

        Assert.Equal("val", value);
    }

    [Fact]
    public async Task Get_Missing_Key_Returns_Null()
    {
        var cache = new InMemoryCacheService();

        var value = await cache.GetAsync<string>("missing", CancellationToken.None);

        Assert.Null(value);
    }

    [Fact]
    public async Task Expired_Entry_Returns_Null()
    {
        var cache = new InMemoryCacheService();

        await cache.SetAsync("k", "val", TimeSpan.FromMilliseconds(-1), CancellationToken.None);

        var value = await cache.GetAsync<string>("k", CancellationToken.None);

        Assert.Null(value);
    }

    [Fact]
    public async Task Delete_Removes_Key()
    {
        var cache = new InMemoryCacheService();

        await cache.SetAsync("k", "val", TimeSpan.FromMinutes(1), CancellationToken.None);
        await cache.DeleteAsync("k", CancellationToken.None);

        Assert.Null(await cache.GetAsync<string>("k", CancellationToken.None));
    }

    [Fact]
    public async Task Delete_By_Prefix_Removes_Matching_Keys()
    {
        var cache = new InMemoryCacheService();

        await cache.SetAsync("products:list:1", "a", TimeSpan.FromMinutes(1), CancellationToken.None);
        await cache.SetAsync("products:list:2", "b", TimeSpan.FromMinutes(1), CancellationToken.None);
        await cache.SetAsync("other", "c", TimeSpan.FromMinutes(1), CancellationToken.None);

        await cache.DeleteByPrefixAsync("products:list", CancellationToken.None);

        Assert.Null(await cache.GetAsync<string>("products:list:1", CancellationToken.None));
        Assert.Null(await cache.GetAsync<string>("products:list:2", CancellationToken.None));
        Assert.Equal("c", await cache.GetAsync<string>("other", CancellationToken.None));
    }

    [Fact]
    public async Task Exists_Reflects_Presence()
    {
        var cache = new InMemoryCacheService();

        await cache.SetAsync("k", "1", TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.True(await cache.ExistsAsync("k", CancellationToken.None));
        Assert.False(await cache.ExistsAsync("missing", CancellationToken.None));
    }
}