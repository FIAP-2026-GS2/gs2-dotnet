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
    public DbSet<CaptureSchedule> CaptureSchedules => Set<CaptureSchedule>();
    public DbSet<Capture> Captures => Set<Capture>();
    public DbSet<Image> Images => Set<Image>();
    public DbSet<AiAnalysis> AiAnalyses => Set<AiAnalysis>();
    public DbSet<AiDetection> AiDetections => Set<AiDetection>();
    public DbSet<IssueReport> IssueReports => Set<IssueReport>();
    public DbSet<IssueHistory> IssueHistories => Set<IssueHistory>();
    public DbSet<IssueAssignment> IssueAssignments => Set<IssueAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
