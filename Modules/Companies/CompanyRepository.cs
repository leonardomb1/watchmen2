using System.Data.Common;
using Watchmen.Common;
using Watchmen.Common.Interfaces;
using Watchmen.Common.Types;
using Watchmen.Modules.Companies.DTO;
using Microsoft.EntityFrameworkCore;

namespace Watchmen.Modules.Companies;

public sealed class CompanyReposity(CompanyDbContext ctx, IConfiguration configuration) :
    IReadRepository<CompanyResponse, Guid>,
    IWriteRepository<CompanyCreation, Guid>,
    IUpdateRepository<CompanyUpdate, Guid>,
    IDeleteRepository<Guid>,
    ISoftDeleteRepository<Guid>
{
    public async ValueTask<Result<CompanyResponse>> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        try
        {
            var company = await ctx.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.PublicId == id, token);

            if (company is null)
                return new Error("Company not found", ErrorType.NotFound);

            var response = new CompanyResponse(
                company.PublicId,
                company.Name,
                company.FiscalCode,
                company.Email,
                company.PhoneNumber,
                company.ContactPerson
            );

            return response;
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Result<PagedQuery<CompanyResponse>>> ListAllAsync(IQueryCollection? query, CancellationToken token = default)
    {
        try
        {
            int page = 1;
            int pageSize = Math.Max(1, configuration.GetValue<int>("PaginationSize"));

            var queryable = ctx.Companies
                .AsNoTracking()
                .OrderByDescending(c => c.InternalId)
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
                        case "fiscalcode":
                            if (value.Length > 100)
                                return new Error("Search pattern too long.", ErrorType.ValidationFailed);
                            queryable = queryable.Where(c => c.FiscalCode == v);
                            break;
                        case "name":
                            if (value.Length > 100)
                                return new Error("Search pattern too long.", ErrorType.ValidationFailed);
                            queryable = queryable.Where(c => EF.Functions.ILike(c.Name, $"%{value}%"));
                            break;
                        case "email":
                            if (value.Length > 100)
                                return new Error("Search pattern too long.", ErrorType.ValidationFailed);
                            queryable = queryable.Where(c => EF.Functions.ILike(c.Email, $"%{value}%"));
                            break;
                        case "contactperson":
                            if (value.Length > 100)
                                return new Error("Search pattern too long.", ErrorType.ValidationFailed);
                            queryable = queryable.Where(c => EF.Functions.ILike(c.ContactPerson, $"%{value}%"));
                            break;
                        case "page":
                            if (int.TryParse(value, out int p))
                                page = Math.Max(1, p);
                            break;
                        case "pagesize":
                            if (int.TryParse(value, out int ps))
                                pageSize = pageSize < Math.Max(1, ps) ? Math.Max(1, ps) : pageSize;
                            break;
                    }
                }
            }

            int totalItems = await queryable.CountAsync(token);

            var projection = await queryable
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CompanyResponse(
                    c.PublicId,
                    c.Name,
                    c.FiscalCode,
                    c.Email,
                    c.PhoneNumber,
                    c.ContactPerson
                ))
                .ToListAsync(token);

            return new PagedQuery<CompanyResponse>(projection, totalItems, page, pageSize);
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Result<Guid>> WriteAsync(CompanyCreation userReq, CancellationToken token = default)
    {
        try
        {
            var validation = userReq.Validate();
            if (!validation.IsSuccess)
                return validation.HasError;

            var newCompany = new CompanyModel
            {
                Name = userReq.Name,
                Address = userReq.Address,
                Email = userReq.Email,
                PhoneNumber = userReq.PhoneNumber,
                ContactPerson = userReq.ContactPerson,
                FiscalCode = userReq.FiscalCode
            };

            await ctx.Companies.AddAsync(newCompany, token);
            await ctx.SaveChangesAsync(token);

            return newCompany.PublicId;
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Attempt> UpdateAsync(CompanyUpdate companyReq, Guid id, CancellationToken token = default)
    {
        try
        {
            var validation = companyReq.Validate();
            if (!validation.IsSuccess)
                return validation.HasError;

            var existingCompany = await ctx.Companies
                .FirstOrDefaultAsync(c => c.PublicId == id, token);

            if (existingCompany is null)
                return new Error("Company not found", ErrorType.NotFound);

            if (companyReq.Name is not null)
                existingCompany.Name = companyReq.Name;

            if (companyReq.Address is not null)
                existingCompany.Address = companyReq.Address;

            if (companyReq.Email is not null)
                existingCompany.Email = companyReq.Email;

            if (companyReq.PhoneNumber is not null)
                existingCompany.PhoneNumber = companyReq.PhoneNumber;

            if (companyReq.ContactPerson is not null)
                existingCompany.ContactPerson = companyReq.ContactPerson;

            existingCompany.UpdatedAt = DateTime.UtcNow;

            ctx.Companies.Update(existingCompany);
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
            int rowsAffected = await ctx.Companies
                .IgnoreQueryFilters()
                .Where(c => c.PublicId == id)
                .ExecuteDeleteAsync(token);

            if (rowsAffected == 0)
                return new Error("Company not found", ErrorType.NotFound);

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
            var company = await ctx.Companies
                .FirstOrDefaultAsync(c => c.PublicId == id, token);

            if (company is null)
                return new Error("Company not found", ErrorType.NotFound);

            company.IsActive = false;
            company.UpdatedAt = DateTime.UtcNow;

            ctx.Companies.Update(company);
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
            var company = await ctx.Companies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.PublicId == id && !c.IsActive, token);

            if (company is null)
                return new Error("Company not found or not deleted", ErrorType.NotFound);

            company.IsActive = true;
            company.UpdatedAt = DateTime.UtcNow;

            ctx.Companies.Update(company);
            await ctx.SaveChangesAsync(token);

            return Attempt.Success();
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }
}