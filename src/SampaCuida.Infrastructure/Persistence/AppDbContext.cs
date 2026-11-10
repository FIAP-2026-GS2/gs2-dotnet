using Microsoft.EntityFrameworkCore;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MonitoringPoint> MonitoringPoints => Set<MonitoringPoint>();
    public DbSet<Camera> Cameras => Set<Camera>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
