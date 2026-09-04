using ECommerce.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/metrics")]
public class AdminMetricsController : ControllerBase
{
    private readonly IRequestMetricsService _metricsService;

    public AdminMetricsController(IRequestMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<PathMetric>> GetMetrics()
    {
        return Ok(new
        {
            totalRequests = _metricsService.TotalRequests(),
            byPath = _metricsService.ByPath()
        });
    }
}