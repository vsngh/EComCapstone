using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders.DTOs;

public record CheckoutRequest(
    string? ShippingAddressLine1,
    string? ShippingAddressLine2,
    string? ShippingCity,
    string? ShippingState,
    string? ShippingPostalCode,
    string? ShippingCountry,
    PaymentMethod PaymentMethod = PaymentMethod.Card,
    string? IdempotencyKey = null);