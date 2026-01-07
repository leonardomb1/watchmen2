using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Watchmen.Common.Abstracts;

public abstract class DbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext> where TContext : DbContext
{
    public TContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        var connectionString = configuration.GetConnectionString("PostgreSql");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'PostgreSql' not found in configuration."
            );
        }

        optionsBuilder.UseNpgsql(connectionString);

        return CreateContext(optionsBuilder.Options);
    }

    protected abstract TContext CreateContext(DbContextOptions<TContext> options);
}