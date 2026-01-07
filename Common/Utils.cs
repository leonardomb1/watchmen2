using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Watchmen.Common.Types;
using Watchmen.Modules.Users;

namespace Watchmen.Common;

public static partial class Utils
{
    public static string GetClientIdentifier(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var firstIp = forwardedFor.Split(',')[0].Trim();
            return firstIp;
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public static IResult MapErrorToHttpResult(Error error) => error.Type switch
    {
        ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
        ErrorType.ValidationFailed => Results.BadRequest(new { error = error.Message }),
        ErrorType.AlreadyExists => Results.Conflict(new { error = error.Message }),
        ErrorType.Unauthorized => Results.Unauthorized(),
        ErrorType.Database => Results.Conflict(new { error = error.Message }),
        ErrorType.Forbidden => Results.Forbid(),
        ErrorType.Configuration => Results.Problem(error.Message),
        _ => Results.Problem(error.Message)
    };

    public static string ComputeHMACSha256Hash(string rawData, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] bytes = Encoding.UTF8.GetBytes(rawData);

        using var hmac = new HMACSHA256(keyBytes);
        byte[] hashBytes = hmac.ComputeHash(bytes);

        return Convert.ToHexString(hashBytes);
    }

    public static Attempt ValidateClaim(HttpContext ctx, Guid userId)
    {
        var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var currentUserId))
            return new Error("Invalid Credentials", ErrorType.Unauthorized);

        if (currentUserId != userId && !ctx.User.IsInRole(UserRole.Admin.ToString()))
            return new Error("Access Denied.", ErrorType.Forbidden);

        return Attempt.Success();
    }

    public static string NormalizeSearchInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string cleaned = CleanSearchRegex().Replace(input, "");

        cleaned = EmptySearchRegex().Replace(cleaned, " ");

        return cleaned.Trim();
    }

    [GeneratedRegex(@"[^a-zA-Z0-9\s\-'\.]")]
    private static partial Regex CleanSearchRegex();
    [GeneratedRegex(@"\s+")]
    private static partial Regex EmptySearchRegex();
}