using Microsoft.EntityFrameworkCore;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MonitoringPoint> MonitoringPoints => Set<MonitoringPoint>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<MunicipalAgency> MunicipalAgencies => Set<MunicipalAgency>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<IssueType> IssueTypes => Set<IssueType>();
    public DbSet<TriggerEvent> TriggerEvents => Set<TriggerEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
