using System.Data.Common;
using Watchmen.Common;
using Watchmen.Common.Interfaces;
using Watchmen.Common.Types;
using Watchmen.Modules.Users.DTO;
using Microsoft.EntityFrameworkCore;

namespace Watchmen.Modules.Users;

public sealed class UserRepository(UserDbContext ctx, IConfiguration configuration) :
    IReadRepository<UserResponse, Guid>,
    IWriteRepository<UserCreation, Guid>,
    IUpdateRepository<UserUpdate, Guid>,
    IDeleteRepository<Guid>,
    ISoftDeleteRepository<Guid>
{
    public async ValueTask<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        try
        {
            var user = await ctx.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PublicId == id, token);

            if (user is null)
                return new Error("User not found", ErrorType.NotFound);

            return new UserResponse(
                user.PublicId,
                user.Name,
                user.Email,
                user.Role.ToString()
            );
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Result<PagedQuery<UserResponse>>> ListAllAsync(IQueryCollection? query, CancellationToken token = default)
    {
        try
        {
            string? hashingKey = configuration["HashingKey"];
            if (string.IsNullOrEmpty(hashingKey))
                return new Error("Hashing key not configured", ErrorType.Configuration);

            int page = 1;
            int pageSize = Math.Max(1, configuration.GetValue<int>("PaginationSize"));

            var queryable = ctx.Users
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

                    if (value.Length > 100)
                        return new Error("Search pattern too long.", ErrorType.ValidationFailed);

                    switch (k.ToLower())
                    {
                        case "name":
                            if (value.Length > 100)
                                return new Error("Search pattern too long.", ErrorType.ValidationFailed);
                            queryable = queryable
                                .Where(x => EF.Functions.ILike(x.Name, $"%{value}%"));
                            break;
                        case "email":
                            if (value.Length > 100)
                                return new Error("Search pattern too long.", ErrorType.ValidationFailed);
                            queryable = queryable
                                .Where(x => EF.Functions.ILike(x.Email, $"%{value}%"));
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
                .Select(e => new UserResponse(
                        e.PublicId,
                        e.Name,
                        e.Email,
                        e.Role.ToString()
                    )
                )
                .ToListAsync(token);

            return new PagedQuery<UserResponse>(projection, totalItems, page, pageSize);
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Result<Guid>> WriteAsync(UserCreation userReq, CancellationToken token)
    {
        try
        {
            if (userReq.Validate().IsFailure)
                return new Error("Invalid user data.", ErrorType.ValidationFailed);

            UserModel user = new()
            {
                Name = userReq.Name,
                Email = userReq.Email,
                Role = Enum.TryParse<UserRole>(userReq.Role, true, out var role) ? role : UserRole.User
            };

            user.SetPassword(userReq.Password);

            await ctx.Users.AddAsync(user, token);
            await ctx.SaveChangesAsync(token);

            return user.PublicId;
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Attempt> UpdateAsync(UserUpdate update, Guid id, CancellationToken token)
    {
        if (update.Validate().IsFailure)
            return new Error("Invalid user data.", ErrorType.ValidationFailed);

        var user = await ctx.Users
                  .FirstOrDefaultAsync(x => x.PublicId == id, token);

        if (user is null)
            return new Error("User not found.", ErrorType.NotFound);

        if (update.Name is not null)
            user.Name = update.Name;

        if (update.Email is not null)
            user.Email = update.Email;

        if (update.Password is not null)
            user.SetPassword(update.Password);

        user.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(token);

        return Attempt.Success();
    }
    public async ValueTask<Attempt> SoftDeleteAsync(Guid id, CancellationToken token)
    {
        var user = await ctx.Users
                    .FirstOrDefaultAsync(x => x.PublicId == id, token);

        if (user is null)
            return new Error("User not found.", ErrorType.NotFound);

        user.IsActive = false;

        user.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(token);

        return Attempt.Success();
    }

    public async ValueTask<Attempt> RestoreAsync(Guid id, CancellationToken token)
    {
        var user = await ctx.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.PublicId == id && !x.IsActive, token);

        if (user is null)
            return new Error("User not found or not deleted.", ErrorType.NotFound);

        user.IsActive = true;

        user.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(token);

        return Attempt.Success();
    }

    public async ValueTask<Attempt> DeleteAsync(Guid id, CancellationToken token = default)
    {
        try
        {
            int rowsAffected = await ctx.Users
                .IgnoreQueryFilters()
                .Where(x => x.PublicId == id)
                .ExecuteDeleteAsync(token);

            if (rowsAffected == 0)
                return new Error("User not found.", ErrorType.NotFound);

            return Attempt.Success();
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }

    public async ValueTask<Result<UserModel>> GetByEmailAsync(string email, CancellationToken token = default)
    {
        try
        {
            var user = await ctx.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == email, token);

            if (user is null)
                return new Error("User not found", ErrorType.NotFound);

            return user;
        }
        catch (DbException ex)
        {
            return new Error(ex.Message, ErrorType.Database);
        }
    }
}