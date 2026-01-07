using Watchmen.Common.Interfaces;
using Watchmen.Common.Types;

namespace Watchmen.Modules.Persons.DTO;

public sealed record PersonCreation(
    string Name,
    string DocumentNumber,
    string? Email,
    string? PhoneNumber
) : IValidatable
{
    public Attempt Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return new Error("Name is required.", ErrorType.ValidationFailed);

        if (string.IsNullOrWhiteSpace(DocumentNumber))
            return new Error("Document number is required.", ErrorType.ValidationFailed);

        return Attempt.Success();
    }
}