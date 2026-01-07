using Watchmen.Common.Interfaces;
using Watchmen.Common.Types;

namespace Watchmen.Modules.Companies.DTO;

public sealed record CompanyCreation(
    string Name,
    string Address,
    string Email,
    string PhoneNumber,
    string ContactPerson,
    string FiscalCode
) : IValidatable
{
    public Attempt Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return new Error("Company name is required", ErrorType.ValidationFailed);

        if (Name.Length > 100)
            return new Error("Company name must not exceed 100 characters", ErrorType.ValidationFailed);

        if (string.IsNullOrWhiteSpace(Address))
            return new Error("Company address is required", ErrorType.ValidationFailed);

        if (Address.Length > 200)
            return new Error("Company address must not exceed 200 characters", ErrorType.ValidationFailed);

        if (string.IsNullOrWhiteSpace(Email))
            return new Error("Company email is required", ErrorType.ValidationFailed);

        if (string.IsNullOrWhiteSpace(PhoneNumber))
            return new Error("Company phone number is required", ErrorType.ValidationFailed);

        if (string.IsNullOrWhiteSpace(ContactPerson))
            return new Error("Contact person is required", ErrorType.ValidationFailed);

        if (string.IsNullOrWhiteSpace(FiscalCode))
            return new Error("Company fiscal code is required", ErrorType.ValidationFailed);


        return Attempt.Success();
    }
}