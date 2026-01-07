using Watchmen.Common.Services;
using Watchmen.Common.Types;
using Watchmen.Modules.Users.DTO;

namespace Watchmen.Modules.Users;

public sealed class UserService(
    UserRepository repo,
    IJwtService jwtService,
    IConfiguration configuration)
{
    private const string SuperAdminId = "00000000-0000-0000-0000-000000000001";

    public async ValueTask<Result<LoginResponse>> LoginAsync(LoginRequest loginRequest, CancellationToken token = default)
    {
        if (TryAuthenticateSuperAdmin(loginRequest, out LoginResponse? res) && res is not null)
            return res;

        var validationResult = loginRequest.Validate();
        if (validationResult.IsFailure)
            return new Error("Invalid Credentials.", ErrorType.Unauthorized);

        var userResult = await repo.GetByEmailAsync(loginRequest.Email, token);

        if (userResult.IsFailure)
            return new Error("Invalid Credentials.", ErrorType.Unauthorized);

        if (!userResult.Value.VerifyPassword(loginRequest.Password))
            return new Error("Invalid Credentials.", ErrorType.Unauthorized);

        UserModel user = userResult.Value;

        var jwtToken = jwtService.GenerateToken(user.PublicId, loginRequest.Email, user.Role.ToString());
        return new LoginResponse(jwtToken);
    }

    private bool TryAuthenticateSuperAdmin(LoginRequest loginRequest, out LoginResponse? token)
    {
        var superAdminEmail = configuration["SuperAdmin:Email"];
        var superAdminPassword = configuration["SuperAdmin:Password"];

        if (string.IsNullOrEmpty(superAdminEmail) || string.IsNullOrEmpty(superAdminPassword))
        {
            token = null;
            return false;
        }

        if (loginRequest.Email != superAdminEmail || loginRequest.Password != superAdminPassword)
        {
            token = null;
            return false;
        }

        var superAdminGuid = Guid.Parse(SuperAdminId);
        var jwtToken = jwtService.GenerateToken(superAdminGuid, superAdminEmail, UserRole.Admin.ToString());
        token = new LoginResponse(jwtToken);

        return true;
    }
}