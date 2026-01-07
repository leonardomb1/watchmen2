namespace Watchmen.Modules.Persons.DTO;

public sealed record PersonResponse(
    Guid PublicId,
    string Name,
    string DocumentNumber,
    string? Email,
    string? PhoneNumber);