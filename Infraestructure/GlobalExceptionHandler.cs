using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace Watchmen.Infraestructure;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        Log.Error(exception, "Unhandled exception occurred: {ExceptionType} - {Message}",
            exception.GetType().Name,
            exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(new
        {
            error = "Sorry, we experienced an internal error in our service."
        }, cancellationToken);

        return true;
    }
}

public static class GlobalExceptionHandlerExtensions
{
    public static IServiceCollection AddGlobalExceptionHandler(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    public static IApplicationBuilder UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(_ => { });
        return app;
    }
}