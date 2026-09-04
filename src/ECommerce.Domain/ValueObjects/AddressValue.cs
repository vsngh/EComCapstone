using ECommerce.Domain.Common;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.ValueObjects;

public sealed record AddressValue
{
    public string Street { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string PostalCode { get; init; }
    public string Country { get; init; }

    public AddressValue(
        string street,
        string city,
        string state,
        string postalCode,
        string country = "India")
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            throw new DomainException("Street is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("City is required.");
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            throw new DomainException("State is required.");
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new DomainException("Postal code is required.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new DomainException("Country is required.");
        }

        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public string FullAddress =>
        $"{Street}, {City}, {State} {PostalCode}, {Country}";
}
