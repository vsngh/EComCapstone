using System.Collections.Concurrent;
using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Notifications;

public sealed record SentEmail(Guid UserId, Guid OrderId, decimal TotalAmount, DateTime SentAt);

public class MockEmailService : IEmailService
{
    private readonly ConcurrentQueue<SentEmail> _sentEmails = new();
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendOrderConfirmationAsync(Guid userId, Guid orderId, decimal totalAmount, CancellationToken cancellationToken)
    {
        var email = new SentEmail(userId, orderId, totalAmount, DateTime.UtcNow);
        _sentEmails.Enqueue(email);

        _logger.LogInformation(
            "Mock email sent to user {UserId} for order {OrderId} totalling {TotalAmount}.",
            userId, orderId, totalAmount);

        return Task.CompletedTask;
    }

    public IReadOnlyList<SentEmail> GetSentEmails()
    {
        return _sentEmails.ToList();
    }
}