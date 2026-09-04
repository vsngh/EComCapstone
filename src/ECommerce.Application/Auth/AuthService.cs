using ECommerce.Application.Auth.DTOs;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Application.Auth;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = new Email(request.Email);

        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
        {
            throw new EmailAlreadyRegisteredException(request.Email);
        }

        var user = User.CreateCustomer(
            request.FirstName,
            request.LastName,
            email,
            _passwordHasher.Hash(request.Password));

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = _tokenService.CreateToken(user.Id, user.Email.Value, user.Role);

        return ToResponse(user, token, expiresAt);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = new Email(request.Email);
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new InvalidCredentialsException();

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var (token, expiresAt) = _tokenService.CreateToken(user.Id, user.Email.Value, user.Role);

        return ToResponse(user, token, expiresAt);
    }

    private static AuthResponse ToResponse(
        User user,
        string token,
        DateTime expiresAt)
    {
        return new AuthResponse(
            token,
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email.Value,
            user.Role.ToString(),
            expiresAt);
    }
}
