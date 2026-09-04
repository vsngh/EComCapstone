using ECommerce.Domain.Common;

namespace ECommerce.Domain.Exceptions;

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid userId)
        : base($"User '{userId}' was not found.")
    {
    }

    public UserNotFoundException(string email)
        : base($"User with email '{email}' was not found.")
    {
    }
}
