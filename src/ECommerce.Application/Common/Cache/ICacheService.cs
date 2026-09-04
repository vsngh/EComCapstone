namespace ECommerce.Application.Common.Cache;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken) where T : class;
    Task DeleteAsync(string key, CancellationToken cancellationToken);
    Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
    Task ClearAsync(CancellationToken cancellationToken);
}