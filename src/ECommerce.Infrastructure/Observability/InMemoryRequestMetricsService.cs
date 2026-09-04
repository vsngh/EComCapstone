using System.Collections.Concurrent;
using ECommerce.Application.Common.Interfaces;

namespace ECommerce.Infrastructure.Observability;

public class InMemoryRequestMetricsService : IRequestMetricsService
{
    private readonly ConcurrentDictionary<string, PathMetric> _byPath = new();
    private int _total;

    public void RecordRequest(string method, string path, int statusCode)
    {
        Interlocked.Increment(ref _total);

        var key = $"{method} {path}";
        _byPath.AddOrUpdate(
            key,
            _ => new PathMetric(method, path, 1),
            (_, existing) => new PathMetric(
                existing.Method,
                existing.Path,
                existing.Calls + 1));
    }

    public int TotalRequests() => Volatile.Read(ref _total);

    public IReadOnlyList<PathMetric> ByPath() => _byPath.Values.ToList();

    public void Reset()
    {
        Interlocked.Exchange(ref _total, 0);
        _byPath.Clear();
    }
}