namespace Watchmen.Modules.Persons.DTO;

public sealed record PersonUpdate(
    string? Name,
    string? DocumentNumber,
    string? Email,
    string? PhoneNumber
);