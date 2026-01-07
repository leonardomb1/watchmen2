using System.Text;
using System.Threading.RateLimiting;
using Watchmen.Common;
using Watchmen.Common.Configuration;
using Watchmen.Common.Services;
using Watchmen.Infraestructure.Middlewares;
using Watchmen.Modules.Companies;
using Watchmen.Modules.Persons;
using Watchmen.Modules.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.OpenTelemetry;

namespace Watchmen.Infraestructure;

public sealed class Server : IAsyncDisposable
{
    private readonly WebApplication app;
    private bool disposed = false;
    public const string APPLICATION_NAME = "Watchmen.Api";
    public const string APPLICATION_VERSION = "1.0.0";
    public Server(string[] args)
    {
        WebApplicationOptions options = new()
        {
            ApplicationName = APPLICATION_NAME,
            Args = args
        };

        var builder = WebApplication.CreateSlimBuilder(options);

        var environment = builder.Environment.EnvironmentName;

        builder
            .Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        var apiKeysConfig = new ConfigurationBuilder()
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("apikeys.json", optional: false, reloadOnChange: true)
            .Build();

        builder.Services.Configure<ApiKeyConfiguration>(apiKeysConfig);

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithSpan()
            .WriteTo.Console()
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpEndpoint;
                options.Protocol = OtlpProtocol.Grpc;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = APPLICATION_NAME,
                };
            })
            .CreateLogger();

        builder.Services.AddGlobalExceptionHandler();
        builder.Services.AddResponseCaching();
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
        });

        builder.Services.AddRequestTimeouts(options =>
        {
            options.DefaultPolicy = new RequestTimeoutPolicy()
            {
                Timeout = TimeSpan.FromSeconds(90)
            };
        });

        builder.Services.AddHealthChecks();

        builder.Services.AddDataProtection();
        builder.Services.AddSingleton<IDataEncryptionService, DataProtectionEncryptionService>();

        builder.Services.AddSingleton<IJwtService, JwtService>();
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)
                )
            };
        });

        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        builder.Services.AddOpenApi();

        builder.Services.AddUsersModule(builder.Configuration);
        builder.Services.AddCompanyModule(builder.Configuration);
        builder.Services.AddPersonModule(builder.Configuration);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: APPLICATION_NAME,
                    serviceVersion: APPLICATION_VERSION
                )
            )
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddSource(APPLICATION_NAME)
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint!);
                    options.Protocol = OtlpExportProtocol.Grpc;
                })
            )
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(APPLICATION_NAME)
                .AddPrometheusExporter()
            );

        int permitLimit = builder.Configuration.GetRequiredSection("RateLimiting").GetValue<int>("PermitLimit");
        int window = builder.Configuration.GetRequiredSection("RateLimiting").GetValue<int>("WindowInMinutes");
        int queueLimit = builder.Configuration.GetRequiredSection("RateLimiting").GetValue<int>("QueueLimit");

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("strict", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: Utils.GetClientIdentifier(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: Utils.GetClientIdentifier(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(window),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = queueLimit
                    });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.",
                    cancellationToken
                );
            };
        });

        builder.Host.UseSerilog();

        app = builder.Build();

        app.UseForwardedHeaders();
        app.UseGlobalExceptionHandler();
        app.UseRequestTimeouts();
        app.UseRateLimiter();
        app.UseResponseCaching();

        app.UseMiddleware<DocumentationAuthMiddleware>();
        app.MapOpenApi()
            .RequireRateLimiting("strict");
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Watchmen API")
                .WithTheme(ScalarTheme.Purple)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        })
            .RequireRateLimiting("strict");

        app.UseMiddleware<ApiKeyMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseResponseCompression();

        app.MapUsersEndpoints();
        app.MapCompanyEndpoints();
        app.MapPersonEndpoints();

        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapPrometheusScrapingEndpoint("/metrics")
            .RequireAuthorization(policy => policy.RequireAssertion(context =>
                context.User.IsInRole("metrics") ||
                context.User.IsInRole(UserRole.Admin.ToString())));
    }

    public async ValueTask RunAsync(CancellationToken token = default)
    {
        Log.Information("Starting {APPLICATION_NAME}", APPLICATION_NAME);
        await app.RunAsync(token);
        await app.StopAsync(token);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    private async ValueTask DisposeAsync(bool disposing)
    {
        if (!disposed && disposing)
        {
            await app.DisposeAsync();
            disposed = true;
        }
    }
}