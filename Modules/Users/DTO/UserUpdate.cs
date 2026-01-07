using System.Net.Mail;
using Watchmen.Common.Interfaces;
using Watchmen.Common.Types;

namespace Watchmen.Modules.Users.DTO;

public sealed record UserUpdate(
    string? Name,
    string? Email,
    string? Password
) : IValidatable
{
    public Attempt Validate()
    {
        if (IsEmpty())
            return new Error("At least one field must be provided for update.", ErrorType.ValidationFailed);

        if (Name is not null)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return new Error("First name cannot be empty.", ErrorType.ValidationFailed);

            if (Name.Length > 100)
                return new Error("First name must not exceed 100 characters.", ErrorType.ValidationFailed);
        }

        if (Name is not null)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return new Error("Last name cannot be empty.", ErrorType.ValidationFailed);

            if (Name.Length > 100)
                return new Error("Last name must not exceed 100 characters.", ErrorType.ValidationFailed);
        }

        if (Email is not null)
        {
            if (string.IsNullOrWhiteSpace(Email))
                return new Error("Email cannot be empty.", ErrorType.ValidationFailed);

            if (Email.Length > 200)
                return new Error("Email must not exceed 200 characters.", ErrorType.ValidationFailed);

            if (!IsValidEmail(Email))
                return new Error("Email format is invalid.", ErrorType.ValidationFailed);
        }

        if (Password is not null)
        {
            if (string.IsNullOrWhiteSpace(Password))
                return new Error("Password cannot be empty.", ErrorType.ValidationFailed);

            if (Password.Length < 8)
                return new Error("Password must be at least 8 characters long.", ErrorType.ValidationFailed);

            if (Password.Length > 128)
                return new Error("Password must not exceed 128 characters.", ErrorType.ValidationFailed);

            if (!HasUpperCase(Password))
                return new Error("Password must contain at least one uppercase letter.", ErrorType.ValidationFailed);

            if (!HasLowerCase(Password))
                return new Error("Password must contain at least one lowercase letter.", ErrorType.ValidationFailed);

            if (!HasDigit(Password))
                return new Error("Password must contain at least one number.", ErrorType.ValidationFailed);
        }

        return Attempt.Success();
    }

    private bool IsEmpty() =>
        Name is null &&
        Email is null &&
        Password is null;

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var mailAddress = new MailAddress(email);
            return mailAddress.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasUpperCase(string str) => str.Any(char.IsUpper);
    private static bool HasLowerCase(string str) => str.Any(char.IsLower);
    private static bool HasDigit(string str) => str.Any(char.IsDigit);
}
