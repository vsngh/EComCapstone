using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class User : Entity
{
    private readonly List<Address> _addresses = new();
    private readonly List<Order> _orders = new();

    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; } = UserRole.Customer;
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    private User()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = new Email("placeholder@example.com");
        PasswordHash = string.Empty;
    }

    private User(
        string firstName,
        string lastName,
        Email email,
        string passwordHash,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("First name is required.");
        }

        FirstName = firstName;
        LastName = lastName ?? string.Empty;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public static User CreateCustomer(
        string firstName,
        string lastName,
        Email email,
        string passwordHash)
    {
        return new User(firstName, lastName, email, passwordHash, UserRole.Customer);
    }

    public static User CreateAdmin(
        string firstName,
        string lastName,
        Email email,
        string passwordHash)
    {
        return new User(firstName, lastName, email, passwordHash, UserRole.Admin);
    }

    public void UpdatePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = passwordHash;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void AddAddress(Address address)
    {
        _addresses.Add(address);
    }
}
