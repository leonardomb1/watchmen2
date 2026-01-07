using Watchmen.Common.Abstracts;
using Microsoft.EntityFrameworkCore;

namespace Watchmen.Modules.Vehicles;

public sealed class VehicleDbContext(DbContextOptions<VehicleDbContext> options) : DbContext(options)
{
    public DbSet<VehicleModel> Vehicles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VehicleModel>()
            .HasQueryFilter(u => u.IsActive);

        modelBuilder.Entity<VehicleModel>()
            .HasIndex(u => u.LicensePlate)
            .IsUnique();

        modelBuilder.Entity<VehicleModel>()
            .HasIndex(u => u.IsActive);
    }
}

public class VehicleDbContextFactory : DbContextFactory<VehicleDbContext>
{
    protected override VehicleDbContext CreateContext(DbContextOptions<VehicleDbContext> options)
    {
        return new VehicleDbContext(options);
    }
}