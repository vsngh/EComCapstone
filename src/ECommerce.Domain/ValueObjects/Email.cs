using System.Text.RegularExpressions;
using ECommerce.Domain.Common;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.ValueObjects;

public sealed partial record Email
{
    public string Value { get; init; }

    public Email(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        value = value.Trim();

        if (!EmailRegex().IsMatch(value))
        {
            throw new DomainException($"'{value}' is not a valid email address.");
        }

        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
