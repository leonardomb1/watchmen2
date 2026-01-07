using Watchmen.Common;
using Watchmen.Common.Types;
using Watchmen.Modules.Companies.DTO;
using Watchmen.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace Watchmen.Modules.Companies;

public static class CompanyModule
{
    public static IServiceCollection AddCompanyModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CompanyDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("PostgreSql")!);
        });

        services.AddScoped<CompanyReposity>();

        return services;
    }

    public static async Task RunMigrationsAsync(IConfiguration configuration)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CompanyDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("PostgreSql")!);

        await using var ctx = new CompanyDbContext(optionsBuilder.Options);

        Console.WriteLine("Applying Company module migrations...");
        await ctx.Database.MigrateAsync();
        Console.WriteLine("Company module migrations applied successfully!");
    }

    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/companies")
            .WithDescription("Operations related to company management")
            .WithTags("Companies");

        group.MapGet("/{id:guid}", async (Guid id, CompanyReposity repo, CancellationToken ct) =>
        {
            var result = await repo.GetByIdAsync(id, ct);
            return result.Match(
               success => success is null
                   ? Results.NotFound()
                   : Results.Ok(success),
               error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Get company by ID")
        .WithDescription("Retrieves a company by its unique identifier.")
        .Produces<CompanyResponse>(200)
        .Produces(401)
        .Produces(404);

        group.MapGet("/", async (HttpContext ctx, CompanyReposity repo, CancellationToken ct) =>
        {
            var result = await repo.ListAllAsync(ctx.Request.Query, ct);

            return result.Match(
                success => success is null
                    ? Results.NotFound()
                    : Results.Ok(success),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("List all companies")
        .WithDescription("Retrieves a list of all companies. Supports filtering via query parameters (e.g., ?name=Acme, ?fiscalCode=123, ?email=contact@company.com). Admin Only")
        .Produces<PagedQuery<CompanyResponse>>(200)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPost("/", async (CompanyCreation request, CompanyReposity repo, CancellationToken ct) =>
        {
            if (request.Validate().IsFailure)
                return Utils.MapErrorToHttpResult(request.Validate().HasError);

            var result = await repo.WriteAsync(request, ct);

            return result.Match(
                success => Results.Created($"/companies/{success}", new { id = success }),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Create a new company")
        .WithDescription("Creates a new company. Requires Admin role.")
        .Accepts<CompanyCreation>("application/json")
        .Produces(201)
        .Produces(400)
        .Produces(401)
        .Produces(409)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPost("/{id:guid}/restore", async (Guid id, CompanyReposity repo, CancellationToken ct) =>
        {
            var result = await repo.RestoreAsync(id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Restore soft deleted company")
        .WithDescription("Restores a company that has been soft deleted. Requires Admin role.")
        .Produces(204)
        .Produces(401)
        .Produces(404)
        .Produces(409)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPut("/{id:guid}", async (Guid id, CompanyUpdate request, CompanyReposity repo, CancellationToken ct) =>
        {
            if (request.Validate().IsFailure)
                return Utils.MapErrorToHttpResult(request.Validate().HasError);

            var result = await repo.UpdateAsync(request, id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Update a company")
        .WithDescription("Updates an existing company's information. Requires Admin role.")
        .Accepts<CompanyUpdate>("application/json")
        .Produces(204)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapDelete("/{id:guid}/permanent", async (Guid id, CompanyReposity repo, CancellationToken ct) =>
        {
            var result = await repo.DeleteAsync(id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Delete a company permanently")
        .WithDescription("Permanently deletes a company from the database. Requires Admin role.")
        .Produces(204)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapDelete("/{id:guid}", async (Guid id, CompanyReposity repo, CancellationToken ct) =>
        {
            var result = await repo.SoftDeleteAsync(id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Soft delete a company")
        .WithDescription("Soft deletes a company (marks as inactive). Requires Admin role.")
        .Produces(204)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        return endpoints;
    }
}