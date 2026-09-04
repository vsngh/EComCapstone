using ECommerce.Domain.Common;

namespace ECommerce.Domain.Exceptions;

public class EmailAlreadyRegisteredException : DomainException
{
    public EmailAlreadyRegisteredException(string email)
        : base($"An account with email '{email}' already exists.")
    {
    }
}
