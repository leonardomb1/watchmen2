using Watchmen.Common.Abstracts;
using Microsoft.EntityFrameworkCore;

namespace Watchmen.Modules.Persons;

public sealed class PersonDbContext(DbContextOptions<PersonDbContext> options) : DbContext(options)
{
    public DbSet<PersonModel> Persons { get; set; } = null!;

    public DbSet<PersonCompanyModel> PersonCompanies { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PersonModel>()
            .HasQueryFilter(u => u.IsActive);

        modelBuilder.Entity<PersonModel>()
            .HasIndex(p => p.IsActive);

        modelBuilder.Entity<PersonModel>()
            .HasIndex(p => p.DocumentNumberHash)
            .IsUnique();

        modelBuilder.Entity<PersonModel>()
            .HasIndex(p => p.EmailHash);

        modelBuilder.Entity<PersonModel>()
            .HasIndex(p => p.PhoneNumberHash);

        modelBuilder.Entity<PersonCompanyModel>()
            .HasQueryFilter(u => u.IsActive);

        modelBuilder.Entity<PersonCompanyModel>()
            .HasIndex(pc => new { pc.PersonInternalId, pc.CompanyInternalId });
    }
}

public class PersonDbContextFactory : DbContextFactory<PersonDbContext>
{
    protected override PersonDbContext CreateContext(DbContextOptions<PersonDbContext> options)
    {
        return new PersonDbContext(options);
    }
}