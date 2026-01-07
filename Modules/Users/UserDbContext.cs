using Watchmen.Common.Abstracts;
using Microsoft.EntityFrameworkCore;

namespace Watchmen.Modules.Users;

public sealed class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
    public DbSet<UserModel> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserModel>()
            .HasQueryFilter(u => u.IsActive);

        modelBuilder.Entity<UserModel>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<UserModel>()
            .HasIndex(u => u.IsActive);
    }
}

public class UserDbContextFactory : DbContextFactory<UserDbContext>
{
    protected override UserDbContext CreateContext(DbContextOptions<UserDbContext> options)
    {
        return new UserDbContext(options);
    }
}