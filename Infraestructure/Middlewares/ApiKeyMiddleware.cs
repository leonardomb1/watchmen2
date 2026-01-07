using Watchmen.Common;
using Watchmen.Common.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using System.Security.Claims;

namespace Watchmen.Infraestructure.Middlewares;

public class ApiKeyMiddleware(
    RequestDelegate next,
    IOptionsMonitor<ApiKeyConfiguration> apiKeysMonitor)
{
    private readonly RequestDelegate next = next;
    private readonly IOptionsMonitor<ApiKeyConfiguration> apiKeysMonitor = apiKeysMonitor;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            var providedKey = apiKeyHeader.ToString();

            var apiKeyConfig = apiKeysMonitor.CurrentValue;

            var validKey = apiKeyConfig.Keys.FirstOrDefault(k =>
                k.IsActive &&
                k.Key == providedKey &&
                (k.ExpiresAt is null || k.ExpiresAt > DateTime.UtcNow));

            if (validKey is not null)
            {
                var claims = new List<Claim>
                  {
                      new(ClaimTypes.Name, validKey.Name),
                      new(ClaimTypes.NameIdentifier, validKey.Id),
                      new("ApiKey", "true"),
                      new("ApiKeyId", validKey.Id)
                  };

                foreach (var scope in validKey.Scopes)
                {
                    claims.Add(new Claim(ClaimTypes.Role, scope));
                }

                var identity = new ClaimsIdentity(claims, "ApiKey");
                context.User = new ClaimsPrincipal(identity);

                Log.Information("API key used: {KeyId} ({KeyName}) for {Path}",
                    validKey.Id, validKey.Name, context.Request.Path);
            }
        }

        await next(context);
    }
}
  