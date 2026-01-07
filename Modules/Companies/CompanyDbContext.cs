using Watchmen.Common.Abstracts;
using Microsoft.EntityFrameworkCore;

namespace Watchmen.Modules.Companies;

public sealed class CompanyDbContext(DbContextOptions<CompanyDbContext> options) : DbContext(options)
{
    public DbSet<CompanyModel> Companies { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyModel>()
            .HasQueryFilter(c => c.IsActive);

        modelBuilder.Entity<CompanyModel>()
            .HasIndex(c => c.FiscalCode)
            .IsUnique();

        modelBuilder.Entity<CompanyModel>()
            .HasIndex(c => c.IsActive);
    }
}

public class CompanyDbContextFactory : DbContextFactory<CompanyDbContext>
{
    protected override CompanyDbContext CreateContext(DbContextOptions<CompanyDbContext> options)
    {
        return new CompanyDbContext(options);
    }
}