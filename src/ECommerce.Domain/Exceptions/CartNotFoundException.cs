using ECommerce.Domain.Common;

namespace ECommerce.Domain.Exceptions;

public class CartNotFoundException : DomainException
{
    public CartNotFoundException(Guid userId)
        : base($"Active cart for user '{userId}' was not found.")
    {
    }
}
