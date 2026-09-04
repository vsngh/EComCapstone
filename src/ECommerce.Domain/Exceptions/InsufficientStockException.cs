namespace ECommerce.Domain.Exceptions;

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(Guid productId, int requested, int available)
        : base(
            $"Insufficient stock for product '{productId}'. Requested {requested} but only {available} available.")
    {
    }
}
