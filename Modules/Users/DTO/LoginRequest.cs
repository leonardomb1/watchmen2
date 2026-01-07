using Watchmen.Common.Interfaces;
using Watchmen.Common.Types;

namespace Watchmen.Modules.Users.DTO;

public sealed record LoginRequest(
    string Email,
    string Password
) : IValidatable
{
    public Attempt Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return new Error("Email is required.", ErrorType.ValidationFailed);

        if (string.IsNullOrWhiteSpace(Password))
            return new Error("Password is required.", ErrorType.ValidationFailed);

        return Attempt.Success();
    }
}