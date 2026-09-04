using System.Collections.Concurrent;

namespace ECommerce.Application.Common.Interfaces;

public interface IRequestMetricsService
{
    void RecordRequest(string method, string path, int statusCode);
    int TotalRequests();
    IReadOnlyList<PathMetric> ByPath();
    void Reset();
}

public sealed record PathMetric(string Method, string Path, int Calls);