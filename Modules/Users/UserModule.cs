using System.Security.Claims;
using Watchmen.Common;
using Watchmen.Common.Types;
using Watchmen.Modules.Users.DTO;
using Microsoft.EntityFrameworkCore;

namespace Watchmen.Modules.Users;

public static class UserModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UserDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("PostgreSql")!);
        });

        services.AddScoped<UserRepository>();
        services.AddScoped<UserService>();

        return services;
    }

    public static async Task RunMigrationsAsync(IConfiguration configuration)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("PostgreSql")!);

        await using var ctx = new UserDbContext(optionsBuilder.Options);

        Console.WriteLine("Applying Users module migrations...");
        await ctx.Database.MigrateAsync();
        Console.WriteLine("Users module migrations applied successfully!");
    }

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users")
            .WithDescription("Operations related to user management")
            .WithTags("Users");

        group.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, UserRepository repo, CancellationToken ct) =>
        {
            var validate = Utils.ValidateClaim(ctx, id);
            if (validate.IsFailure)
                return Utils.MapErrorToHttpResult(validate.HasError);

            var result = await repo.GetByIdAsync(id, ct);

            return result.Match(
                success => success is null
                    ? Results.NotFound()
                    : Results.Ok(success),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Get user by ID")
        .WithDescription("Retrieves a user by their unique identifier. Users can only access their own data unless they have Admin role.")
        .Produces<UserResponse>(200)
        .Produces(401)
        .Produces(403)
        .Produces(404);

        group.MapGet("/me", async (HttpContext ctx, UserRepository repo, CancellationToken ct) =>
        {
            var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var currentUserId))
                return Results.Unauthorized();

            var result = await repo.GetByIdAsync(currentUserId, ct);

            return result.Match(
                success => Results.Ok(success),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Gets the current user")
        .WithDescription("Retrieves the current logged in user information.")
        .Produces<UserResponse>(200)
        .Produces(401);

        group.MapGet("/", async (HttpContext ctx, UserRepository repo, CancellationToken ct) =>
        {
            var result = await repo.ListAllAsync(ctx.Request.Query, ct);

            return result.Match(
                success => success is null
                    ? Results.NotFound()
                    : Results.Ok(success),
                error => Utils.MapErrorToHttpResult(error)
                );
        })
        .WithSummary("List all users")
        .WithDescription("Retrieves a list of all users. Supports filtering via query parameters (e.g., ?firstName=John). Admin Only")
        .Produces<PagedQuery<UserResponse>>(200)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPost("/", async (UserCreation request, UserRepository repo, CancellationToken ct) =>
        {
            if (request.Validate().IsFailure)
                return Utils.MapErrorToHttpResult(request.Validate().HasError);

            var result = await repo.WriteAsync(request, ct);

            return result.Match(
                success => Results.Created($"/users/{success}", new { id = success }),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Create a new user")
        .WithDescription("Creates a new user account. Requires Admin role.")
        .Accepts<UserCreation>("application/json")
        .Produces(201)
        .Produces(400)
        .Produces(401)
        .Produces(409)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPost("/{id:guid}/restore", async (Guid id, UserRepository repo, CancellationToken ct) =>
        {
            var result = await repo.RestoreAsync(id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Restores soft deleted user")
        .WithDescription("Restores a user that has been soft deleted. Requires Admin role.")
        .Accepts<UserCreation>("application/json")
        .Produces(204)
        .Produces(401)
        .Produces(403)
        .Produces(409)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapPut("/{id:guid}", async (HttpContext ctx, Guid id, UserUpdate request, UserRepository repo, CancellationToken ct) =>
        {
            var validate = Utils.ValidateClaim(ctx, id);
            if (validate.IsFailure)
                return Utils.MapErrorToHttpResult(validate.HasError);

            if (request.Validate().IsFailure)
                return Utils.MapErrorToHttpResult(request.Validate().HasError);

            var result = await repo.UpdateAsync(request, id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Update a user")
        .WithDescription("Updates an existing user's information. Requires Admin role.")
        .Accepts<UserCreation>("application/json")
        .Produces(204)
        .Produces(400)
        .Produces(401)
        .Produces(404);

        group.MapDelete("/{id:guid}/permanent", async (Guid id, UserRepository repo, CancellationToken ct) =>
        {
            var result = await repo.DeleteAsync(id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Delete a user")
        .WithDescription("Deletes a user permanently. Requires Admin role.")
        .Produces(204)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

        group.MapDelete("/{id:guid}", async (HttpContext ctx, Guid id, UserRepository repo, CancellationToken ct) =>
        {
            var validate = Utils.ValidateClaim(ctx, id);
            if (validate.IsFailure)
                return Utils.MapErrorToHttpResult(validate.HasError);

            var result = await repo.SoftDeleteAsync(id, ct);

            return result.Match(
                () => Results.NoContent(),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("Delete a user")
        .WithDescription("Soft Deletes a user.")
        .Produces(204)
        .Produces(401)
        .Produces(404);

        group.MapPost("/login", async (LoginRequest request, UserService repo, CancellationToken ct) =>
        {
            var result = await repo.LoginAsync(request, ct);

            return result.Match(
                success => Results.Ok(success),
                error => Utils.MapErrorToHttpResult(error)
            );
        })
        .WithSummary("User login")
        .WithDescription("Authenticates a user and returns a JWT token. Public endpoint.")
        .Accepts<LoginRequest>("application/json")
        .Produces<LoginResponse>(200)
        .Produces(400)
        .Produces(401)
        .AllowAnonymous()
        .RequireRateLimiting("strict");

        return app;
    }
}