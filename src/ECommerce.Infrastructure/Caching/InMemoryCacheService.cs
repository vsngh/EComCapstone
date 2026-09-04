using ECommerce.Application.Common.Cache;

namespace ECommerce.Infrastructure.Caching;

public class InMemoryCacheService : ICacheService
{
    private sealed record CacheEntry(object? Value, DateTime ExpiresAt);

    private readonly Dictionary<string, CacheEntry> _store = new();
    private readonly object _lock = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            if (!_store.TryGetValue(key, out var entry))
            {
                return Task.FromResult<T?>(default);
            }

            if (entry.ExpiresAt <= now)
            {
                _store.Remove(key);
                return Task.FromResult<T?>(default);
            }

            return Task.FromResult((T?)entry.Value);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken) where T : class
    {
        lock (_lock)
        {
            _store[key] = new CacheEntry(value, DateTime.UtcNow.Add(ttl));
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _store.Remove(key);
        }

        return Task.CompletedTask;
    }

    public Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var matches = _store.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var match in matches)
            {
                _store.Remove(match);
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            if (!_store.TryGetValue(key, out var entry))
            {
                return Task.FromResult(false);
            }

            if (entry.ExpiresAt <= now)
            {
                _store.Remove(key);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _store.Clear();
        }

        return Task.CompletedTask;
    }
}