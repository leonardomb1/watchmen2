namespace Watchmen.Modules.Users.DTO;

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role
);