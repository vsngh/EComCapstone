using ECommerce.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/outbox")]
public class AdminOutboxController : ControllerBase
{
    private readonly IOutboxProcessingService _outboxProcessingService;

    public AdminOutboxController(IOutboxProcessingService outboxProcessingService)
    {
        _outboxProcessingService = outboxProcessingService;
    }

    [HttpPost("process")]
    public async Task<ActionResult<OutboxProcessingResult>> ProcessPending(
        [FromQuery] int batchSize,
        [FromQuery] int maxAttempts,
        CancellationToken cancellationToken)
    {
        var result = await _outboxProcessingService.ProcessPendingAsync(
            batchSize > 0 ? batchSize : 100,
            maxAttempts > 0 ? maxAttempts : 3,
            cancellationToken);

        return Ok(result);
    }
}