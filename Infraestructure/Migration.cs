using Watchmen.Modules.Companies;
using Watchmen.Modules.Persons;
using Watchmen.Modules.Users;

namespace Watchmen.Infraestructure;

public static class Migration
{
    public static async Task RunMigrationsAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder
            .Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        Console.WriteLine("Starting database migrations...");
        Console.WriteLine("================================");

        await UserModule.RunMigrationsAsync(builder.Configuration);
        await CompanyModule.RunMigrationsAsync(builder.Configuration);
        await PersonModule.RunMigrationsAsync(builder.Configuration);

        Console.WriteLine("================================");
        Console.WriteLine("All migrations completed successfully!");
    }
}