using System.Diagnostics;
using ECommerce.Application.Common.Interfaces;

namespace ECommerce.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly IRequestMetricsService _metricsService;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IRequestMetricsService metricsService)
    {
        _next = next;
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString();

        context.TraceIdentifier = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        _logger.LogInformation(
            "Handling request {CorrelationId} {Method} {Path}",
            correlationId,
            context.Request.Method,
            context.Request.Path);

        await _next(context);

        stopwatch.Stop();
        _metricsService.RecordRequest(
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode);

        _logger.LogInformation(
            "Handled request {CorrelationId} {Method} {Path} -> {StatusCode} in {ElapsedMs}ms",
            correlationId,
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}
