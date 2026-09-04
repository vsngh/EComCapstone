using System.Security.Claims;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(
        Guid userId,
        string email,
        UserRole role);
}
