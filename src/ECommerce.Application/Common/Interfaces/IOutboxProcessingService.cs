namespace ECommerce.Application.Common.Interfaces;

public sealed record OutboxProcessingResult(int Total, int Processed, int Failed);

public interface IOutboxProcessingService
{
    Task<OutboxProcessingResult> ProcessPendingAsync(
        int batchSize,
        int maxAttempts,
        CancellationToken cancellationToken);
}