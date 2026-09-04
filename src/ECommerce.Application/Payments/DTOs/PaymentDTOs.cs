namespace ECommerce.Application.Payments.DTOs;

public record PaymentRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    Domain.Enums.PaymentMethod Method,
    string? IdempotencyKey);

public record PaymentResult(
    bool Succeeded,
    string? ProviderReference,
    string? FailureReason);

public sealed record PaymentResponse(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string Status,
    string? ProviderReference,
    string? FailureReason,
    DateTime CreatedAt);

public sealed record PaymentWebhookRequest(
    string EventType,
    string ProviderReference,
    bool Succeeded,
    string? FailureReason);