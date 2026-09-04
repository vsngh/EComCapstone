namespace ECommerce.Domain.Enums;

public enum OrderStatus
{
    Pending = 1,
    PaymentProcessing = 2,
    Confirmed = 3,
    Packed = 4,
    Shipped = 5,
    Delivered = 6,
    Cancelled = 7,
    PaymentFailed = 8
}
