using System.Data.Common;
using Watchmen.Common;
using Watchmen.Common.Interfaces;
using Watchmen.Common.Services;
using Watchmen.Common.Types;
using Watchmen.Modules.Companies;
using Watchmen.Modules.Persons.DTO;
using Microsoft.EntityFrameworkCore;

namespace Watchmen.Modules.Persons;

public sealed class PersonRepository(PersonDbContext ctx, CompanyDbContext companyCtx, IDataEncryptionService encryption, IConfiguration configuration) :
    IReadRepository<PersonResponse, Guid>,
    IWriteRepository<PersonCreation, Guid>,
    IUpdateRepository<PersonUpdate, Guid>,
    IDeleteRepository<Guid>,
    ISoftDeleteRepository<Guid>
{
    private const string emailCypherPurpose = "Watchmen.Users.Email.v1";
    private const string phoneCypherPurpose = "Watchmen.Users.Phone.v1";
    private const string documentCypherPurpose = "Watchmen.Users.DocumentNumber.v1";
    private readonly string? hashingKey = configuration["HashingKey"];
    public async ValueTask<Result<PersonResponse>> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        try
        {
            var person = await ctx.Persons
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PublicId == id, token);

            if (person is null)
                return new Error("Person not found", ErrorType.NotFound);

            string decryptedDocument = encryption.Decrypt(person.DocumentNumber, documentCypherPurpose);
            string? decryptedEmail = person.Email is not null ? encryption.Decrypt(person.Email, emailCypherPurpose) : null;
            string? decryptedPhone = person.PhoneNumber is not null ? encryption.Decrypt(person.PhoneNumber, phoneCypherPurpose) : null;

            return new PersonResponse(
                person.PublicId,
                person.Name,
                decryptedDocument,
                decryptedEmail,
                decryptedPhone
            );
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Result<PagedQuery<PersonResponse>>> ListAllAsync(IQueryCollection? query, CancellationToken token = default)
    {
        try
        {
            if (string.IsNullOrEmpty(hashingKey))
                return new Error("Hashing key not configured", ErrorType.Configuration);

            int page = 1;
            int pageSize = Math.Max(1, configuration.GetValue<int>("PaginationSize"));

            var queryable = ctx.Persons
                .AsNoTracking()
                .OrderByDescending(e => e.InternalId)
                .AsQueryable();

            if (query is not null)
            {
                foreach (var (k, v) in query)
                {
                    if (string.IsNullOrWhiteSpace(v))
                        continue;

                    string value = Utils.NormalizeSearchInput(v.ToString());

                    switch (k.ToLower())
                    {
                        case "name":
                            if (value.Length > 100)
                                return new Error("Search pattern too long.", ErrorType.ValidationFailed);
                            queryable = queryable.Where(x => EF.Functions.ILike(x.Name, $"%{value}%"));
                            break;
                        case "documentnumber":
                            if (value.Length > 100)
                                return new Error("Search pattern too long.", ErrorType.ValidationFailed);
                            queryable = queryable.Where(x => x.DocumentNumberHash == Utils.ComputeHMACSha256Hash(value, hashingKey));
                            break;
                        case "email":
                            if (value.Length > 100)
                                return new Error("Search pattern too long.", ErrorType.ValidationFailed);
                            queryable = queryable.Where(x => x.EmailHash == Utils.ComputeHMACSha256Hash(value, hashingKey));
                            break;
                        case "phonenumber":
                            if (value.Length > 100)
                                return new Error("Search pattern too long.", ErrorType.ValidationFailed);
                            queryable = queryable.Where(e => e.PhoneNumberHash == Utils.ComputeHMACSha256Hash(value, hashingKey));
                            break;
                        case "page":
                            if (int.TryParse(value, out int p))
                                page = Math.Max(1, p);
                            break;
                        case "pagesize":
                            if (int.TryParse(value, out int ps))
                                pageSize = Math.Max(1, ps);
                            break;
                    }
                }
            }

            int totalItems = await queryable.CountAsync(token);

            var projection = await queryable
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(person => new
                {
                    person.PublicId,
                    person.Name,
                    person.DocumentNumber,
                    person.Email,
                    person.PhoneNumber
                })
                .ToListAsync(token);

            List<PersonResponse> persons = [.. projection.Select(x => new PersonResponse(
                x.PublicId,
                x.Name,
                x.DocumentNumber is not null ? encryption.Decrypt(x.DocumentNumber, documentCypherPurpose) : "",
                x.Email is not null ? encryption.Decrypt(x.Email, emailCypherPurpose) : null,
                x.PhoneNumber is not null ? encryption.Decrypt(x.PhoneNumber, phoneCypherPurpose) : null
            ))];

            return new PagedQuery<PersonResponse>(persons, totalItems, page, pageSize);
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Result<Guid>> WriteAsync(PersonCreation personReq, CancellationToken token = default)
    {
        try
        {
            var validationResult = personReq.Validate();
            if (validationResult.IsFailure)
                return validationResult.HasError;

            if (string.IsNullOrEmpty(hashingKey))
                return new Error("Hashing key not configured", ErrorType.Configuration);

            PersonModel person = new()
            {
                Name = personReq.Name,
                DocumentNumber = encryption.Encrypt(personReq.DocumentNumber, documentCypherPurpose),
                DocumentNumberHash = Utils.ComputeHMACSha256Hash(personReq.DocumentNumber, hashingKey),
                Email = personReq.Email is not null ? encryption.Encrypt(personReq.Email, emailCypherPurpose) : null,
                EmailHash = personReq.Email is not null ? Utils.ComputeHMACSha256Hash(personReq.Email, hashingKey) : null,
                PhoneNumber = personReq.PhoneNumber is not null ? encryption.Encrypt(personReq.PhoneNumber, phoneCypherPurpose) : null,
                PhoneNumberHash = personReq.PhoneNumber is not null ? Utils.ComputeHMACSha256Hash(personReq.PhoneNumber, hashingKey) : null
            };

            await ctx.Persons.AddAsync(person, token);
            await ctx.SaveChangesAsync(token);

            return person.PublicId;
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Attempt> UpdateAsync(PersonUpdate update, Guid id, CancellationToken token = default)
    {
        try
        {
            if (string.IsNullOrEmpty(hashingKey))
                return new Error("Hashing key not configured", ErrorType.Configuration);

            var person = await ctx.Persons
                      .FirstOrDefaultAsync(x => x.PublicId == id, token);

            if (person is null)
                return new Error("Person not found.", ErrorType.NotFound);

            if (update.Name is not null)
                person.Name = update.Name;

            if (update.DocumentNumber is not null)
            {
                person.DocumentNumber = encryption.Encrypt(update.DocumentNumber, documentCypherPurpose);
                person.DocumentNumberHash = Utils.ComputeHMACSha256Hash(update.DocumentNumber, hashingKey);
            }

            if (update.Email is not null)
            {
                person.Email = encryption.Encrypt(update.Email, emailCypherPurpose);
                person.EmailHash = Utils.ComputeHMACSha256Hash(update.Email, hashingKey);
            }

            if (update.PhoneNumber is not null)
            {
                person.PhoneNumber = encryption.Encrypt(update.PhoneNumber, phoneCypherPurpose);
                person.PhoneNumberHash = Utils.ComputeHMACSha256Hash(update.PhoneNumber, hashingKey);
            }

            person.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync(token);

            return Attempt.Success();
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Attempt> SoftDeleteAsync(Guid id, CancellationToken token = default)
    {
        try
        {
            var person = await ctx.Persons
                      .FirstOrDefaultAsync(x => x.PublicId == id, token);

            if (person is null)
                return new Error("Person not found.", ErrorType.NotFound);

            person.IsActive = false;
            person.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync(token);

            return Attempt.Success();
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Attempt> RestoreAsync(Guid id, CancellationToken token = default)
    {
        try
        {
            var person = await ctx.Persons
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.PublicId == id && !x.IsActive, token);

            if (person is null)
                return new Error("Person not found or not deleted.", ErrorType.NotFound);

            person.IsActive = true;
            person.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync(token);

            return Attempt.Success();
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Attempt> DeleteAsync(Guid id, CancellationToken token = default)
    {
        try
        {
            int rowsAffected = await ctx.Persons
                .IgnoreQueryFilters()
                .Where(x => x.PublicId == id)
                .ExecuteDeleteAsync(token);

            if (rowsAffected == 0)
                return new Error("Person not found.", ErrorType.NotFound);

            return Attempt.Success();
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Attempt> AssociatePersonToCompanyAsync(Guid personId, Guid companyId, CancellationToken token = default)
    {
        try
        {
            await using var transaction = await ctx.Database.BeginTransactionAsync(token);

            var person = await ctx.Persons
                .AsNoTracking()
                .Select(x => new { x.InternalId, x.PublicId })
                .FirstOrDefaultAsync(x => x.PublicId == personId, token);

            if (person is null)
                return new Error("Person not found.", ErrorType.NotFound);

            var company = await companyCtx.Companies
                .AsNoTracking()
                .Select(x => new { x.InternalId, x.PublicId })
                .FirstOrDefaultAsync(x => x.PublicId == companyId, token);

            if (company is null)
                return new Error("Company not found.", ErrorType.NotFound);

            var existingAssociation = await ctx.PersonCompanies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(pc =>
                    pc.PersonInternalId == person.InternalId &&
                    pc.CompanyInternalId == company.InternalId,
                    token);

            if (existingAssociation is not null && existingAssociation.IsActive)
                return new Error("Association already exists.", ErrorType.AlreadyExists);

            await ctx.PersonCompanies
                .Where(pc => pc.PersonInternalId == person.InternalId && pc.IsActive)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(pc => pc.IsActive, false),
                    token);

            if (existingAssociation is not null)
            {
                existingAssociation.IsActive = true;
                ctx.PersonCompanies.Update(existingAssociation);
            }
            else
            {
                var personCompany = new PersonCompanyModel
                {
                    PersonInternalId = person.InternalId,
                    CompanyInternalId = company.InternalId
                };
                await ctx.PersonCompanies.AddAsync(personCompany, token);
            }

            await ctx.SaveChangesAsync(token);
            await transaction.CommitAsync(token);

            return Attempt.Success();
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }
}