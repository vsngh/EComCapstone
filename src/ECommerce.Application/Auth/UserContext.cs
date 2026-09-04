using ECommerce.Domain.Exceptions;

namespace ECommerce.Application.Auth;

public interface IUserContext
{
    Guid GetRequiredUserId();
    string GetRequiredEmail();
    bool IsInRole(string role);
}

public class UserContext : IUserContext
{
    private readonly ICurrentUser _currentUser;

    public UserContext(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public Guid GetRequiredUserId()
    {
        if (_currentUser.UserId is null)
        {
            throw new UserNotFoundException(Guid.Empty);
        }

        return _currentUser.UserId.Value;
    }

    public string GetRequiredEmail()
    {
        return _currentUser.Email
            ?? throw new UserNotFoundException(Guid.Empty);
    }

    public bool IsInRole(string role)
    {
        return string.Equals(_currentUser.Role, role, StringComparison.OrdinalIgnoreCase);
    }
}
