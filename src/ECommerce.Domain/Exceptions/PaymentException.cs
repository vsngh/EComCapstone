namespace ECommerce.Domain.Exceptions;

public class PaymentException : DomainException
{
    public PaymentException(string message)
        : base(message)
    {
    }

    public PaymentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
