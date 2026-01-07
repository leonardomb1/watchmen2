using Watchmen.Common.Interfaces;
using Watchmen.Common.Types;

namespace Watchmen.Modules.Companies.DTO;

// A sealed record used for updating company details with validation logic wimilar to the users

public sealed record CompanyUpdate(
    string? Name,
    string? Address,
    string? Email,
    string? PhoneNumber,
    string? ContactPerson,
    string? FiscalCode
) : IValidatable
{
    public Attempt Validate()
    {
        if (IsEmpty())
            return new Error("At least one field must be provided for update", ErrorType.ValidationFailed);

        if (Name is not null)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return new Error("Company name cannot be empty", ErrorType.ValidationFailed);

            if (Name.Length > 100)
                return new Error("Company name must not exceed 100 characters", ErrorType.ValidationFailed);
        }

        if (Address is not null)
        {
            if (string.IsNullOrWhiteSpace(Address))
                return new Error("Company address cannot be empty", ErrorType.ValidationFailed);

            if (Address.Length > 200)
                return new Error("Company address must not exceed 200 characters", ErrorType.ValidationFailed);
        }

        if (Email is not null)
        {
            if (string.IsNullOrWhiteSpace(Email))
                return new Error("Company email cannot be empty", ErrorType.ValidationFailed);
        }

        if (PhoneNumber is not null)
        {
            if (string.IsNullOrWhiteSpace(PhoneNumber))
                return new Error("Company phone number cannot be empty", ErrorType.ValidationFailed);
        }

        if (ContactPerson is not null)
        {
            if (string.IsNullOrWhiteSpace(ContactPerson))
                return new Error("Contact person cannot be empty", ErrorType.ValidationFailed);
        }

        if (FiscalCode is not null)
        {
            if (string.IsNullOrWhiteSpace(FiscalCode))
                return new Error("Company fiscal code cannot be empty", ErrorType.ValidationFailed);
        }

        return Attempt.Success();
    }

    private bool IsEmpty() =>
        Name is null &&
        Address is null &&
        Email is null &&
        PhoneNumber is null &&
        ContactPerson is null &&
        FiscalCode is null;
}