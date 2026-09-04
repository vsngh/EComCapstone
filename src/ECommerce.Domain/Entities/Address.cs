using ECommerce.Domain.Common;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public class Address : Entity
{
    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public AddressValue AddressValue { get; private set; }
    public bool IsDefault { get; private set; }

    private Address()
    {
        AddressValue = new AddressValue("", "", "", "", "");
    }

    public Address(
        Guid userId,
        AddressValue addressValue,
        bool isDefault = false)
    {
        UserId = userId;
        AddressValue = addressValue;
        IsDefault = isDefault;
    }

    public void MarkAsDefault()
    {
        IsDefault = true;
    }

    public void ClearDefault()
    {
        IsDefault = false;
    }
}
