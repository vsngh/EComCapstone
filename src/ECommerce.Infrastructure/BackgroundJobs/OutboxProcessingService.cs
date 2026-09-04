using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Messaging;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.BackgroundJobs;

public class OutboxProcessingService : IOutboxProcessingService
{
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly IEventDispatcher _dispatcher;
    private readonly ILogger<OutboxProcessingService> _logger;

    public OutboxProcessingService(
        IOutboxMessageRepository outboxRepository,
        IEventDispatcher dispatcher,
        ILogger<OutboxProcessingService> logger)
    {
        _outboxRepository = outboxRepository;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<OutboxProcessingResult> ProcessPendingAsync(
        int batchSize,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        var messages = await _outboxRepository.GetUnprocessedAsync(batchSize, cancellationToken);

        var processed = 0;
        var failed = 0;

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await _dispatcher.DispatchAsync(message.EventType, message.Payload, cancellationToken);
                await _outboxRepository.MarkProcessedAsync(message, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(
                    "Failed to process outbox message {MessageId} ({EventType}) after attempt {Attempt}: {Error}",
                    message.Id, message.EventType, message.RetryCount + 1, ex.Message);

                await _outboxRepository.IncrementRetryCountAsync(message, ex.Message, cancellationToken);
            }
        }

        return new OutboxProcessingResult(messages.Count, processed, failed);
    }
}