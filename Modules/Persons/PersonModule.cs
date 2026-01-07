using Watchmen.Common;
using Watchmen.Common.Types;
using Watchmen.Modules.Persons.DTO;
using Watchmen.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace Watchmen.Modules.Persons;

public static class PersonModule
{
    public static IServiceCollection AddPersonModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PersonDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("PostgreSql")!);
        });

        services.AddScoped<PersonRepository>();

        return services;
    }

    public static async Task RunMigrationsAsync(IConfiguration configuration)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PersonDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("PostgreSql")!);

        await using var ctx = new PersonDbContext(optionsBuilder.Options);

        Console.WriteLine("Applying Persons module migrations...");
        await ctx.Database.MigrateAsync();
        Console.WriteLine("Persons module migrations applied successfully!");
    }

    public static IEndpointRouteBuilder MapPersonEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/persons")
            .WithDescription("Operations related to person management")
            .WithTags("Persons");

        group.MapGet("/{id:guid}", async (Guid id, PersonRepository repo, CancellationToken ct) =>
        {
            var result = await repo.GetByIdAsync(id, ct);

            return result.Match(
                success => success is null
                    ? Results.NotFound()
                    : Results.Ok(success),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Get person by ID")
        .WithDescription("Retrieves a person by their unique identifier.")
        .Produces<PersonResponse>(200)
        .Produces(401)
        .Produces(404);

        group.MapGet("/", async (HttpContext ctx, PersonRepository repo, CancellationToken ct) =>
        {
            var result = await repo.ListAllAsync(ctx.Request.Query, ct);

            return result.Match(
                success => success is null
                    ? Results.NotFound()
                    : Results.Ok(success),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("List all persons")
        .WithDescription("Retrieves a list of all persons. Supports filtering via query parameters (e.g., ?name=John, ?documentnumber=123, ?email=john@example.com, ?phonenumber=555-1234). Admin Only")
        .Produces<PagedQuery<PersonResponse>>(200)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPost("/", async (PersonCreation request, PersonRepository repo, CancellationToken ct) =>
        {
            if (request.Validate().IsFailure)
                return Utils.MapErrorToHttpResult(request.Validate().HasError);

            var result = await repo.WriteAsync(request, ct);

            return result.Match(
                success => Results.Created($"/persons/{success}", new { id = success }),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Create a new person")
        .WithDescription("Creates a new person record. Requires Admin role.")
        .Accepts<PersonCreation>("application/json")
        .Produces(201)
        .Produces(400)
        .Produces(401)
        .Produces(409)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPost("/{id:guid}/restore", async (Guid id, PersonRepository repo, CancellationToken ct) =>
        {
            var result = await repo.RestoreAsync(id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Restore soft deleted person")
        .WithDescription("Restores a person that has been soft deleted. Requires Admin role.")
        .Produces(204)
        .Produces(401)
        .Produces(404)
        .Produces(409)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPost("/{personId:guid}/associate/{companyId:guid}", async (Guid personId, Guid companyId, PersonRepository repo, CancellationToken ct) =>
        {
            var result = await repo.AssociatePersonToCompanyAsync(personId, companyId, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Associate person to company")
        .WithDescription("Associates a person with a company. Only one company can be active per person at a time. Requires Admin role.")
        .Produces(204)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .Produces(409)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPut("/{id:guid}", async (Guid id, PersonUpdate request, PersonRepository repo, CancellationToken ct) =>
        {
            var result = await repo.UpdateAsync(request, id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Update a person")
        .WithDescription("Updates an existing person's information. Requires Admin role.")
        .Accepts<PersonUpdate>("application/json")
        .Produces(204)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapDelete("/{id:guid}/permanent", async (Guid id, PersonRepository repo, CancellationToken ct) =>
        {
            var result = await repo.DeleteAsync(id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Delete a person permanently")
        .WithDescription("Permanently deletes a person from the database. Requires Admin role.")
        .Produces(204)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapDelete("/{id:guid}", async (Guid id, PersonRepository repo, CancellationToken ct) =>
        {
            var result = await repo.SoftDeleteAsync(id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Soft delete a person")
        .WithDescription("Soft deletes a person (marks as inactive). Requires Admin role.")
        .Produces(204)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        return endpoints;
    }
}
