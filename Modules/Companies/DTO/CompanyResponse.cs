namespace Watchmen.Modules.Companies.DTO;

public sealed record CompanyResponse(
    Guid Id,
    string Name,
    string FiscalCode,
    string Email,
    string PhoneNumber,
    string ContactPerson
);