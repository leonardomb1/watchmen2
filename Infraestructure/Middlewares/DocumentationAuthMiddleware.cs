using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Watchmen.Modules.Users;
using Watchmen.Modules.Users.DTO;

namespace Watchmen.Infraestructure.Middlewares;

public class DocumentationAuthMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate next = next;

    public async Task InvokeAsync(HttpContext context, UserService userService)
    {
        if (context.Request.Path.StartsWithSegments("/scalar") ||
            context.Request.Path.StartsWithSegments("/openapi"))
        {
            string? token = null;

            if (context.Request.Cookies.TryGetValue("ScalarAuthToken", out var cookieToken))
            {
                token = cookieToken;
            }
            else if (context.Request.Headers.Authorization.ToString() is string authHeader &&
                     !string.IsNullOrEmpty(authHeader) &&
                     authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader["Bearer ".Length..].Trim();
            }
            else if (context.Request.Headers.Authorization.ToString() is string basicHeader &&
                     !string.IsNullOrEmpty(basicHeader) &&
                     basicHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                string encodedCredentials = basicHeader["Basic ".Length..].Trim();
                string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                string[] parts = credentials.Split(':', 2);

                if (parts.Length == 2)
                {
                    LoginRequest loginRequest = new(parts[0], parts[1]);
                    var cred = await userService.LoginAsync(loginRequest);
                    if (cred.IsSuccess)
                    {
                        token = cred.Value.Token;

                        context.Response.Cookies.Append("ScalarAuthToken", token, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTimeOffset.UtcNow.AddHours(8)
                        });
                    }
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);
                    var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

                    if (roleClaim?.Value == UserRole.Admin.ToString())
                    {
                        context.Request.Headers.Authorization = $"Bearer {token}";
                        await next(context);
                        return;
                    }

                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Access forbidden: Admin role required");
                    return;
                }
            }

            context.Response.StatusCode = 401;
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Scalar API Documentation\"";
            await context.Response.WriteAsync("Authentication required");
            return;
        }

        await next(context);
    }
}
