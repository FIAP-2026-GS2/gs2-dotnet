using Microsoft.EntityFrameworkCore;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Application.Abstractions;

/// <summary>
/// O que o Application precisa do banco. Ele conhece o EF Core, mas não o SQL Server:
/// a implementação (AppDbContext) fica no Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<MonitoringPoint> MonitoringPoints { get; }
    DbSet<Camera> Cameras { get; }
    DbSet<MunicipalAgency> MunicipalAgencies { get; }
    DbSet<Team> Teams { get; }
    DbSet<IssueType> IssueTypes { get; }
    DbSet<TriggerEvent> TriggerEvents { get; }
    DbSet<CaptureSchedule> CaptureSchedules { get; }
    DbSet<Capture> Captures { get; }
    DbSet<Image> Images { get; }
    DbSet<AiAnalysis> AiAnalyses { get; }
    DbSet<AiDetection> AiDetections { get; }
    DbSet<IssueReport> IssueReports { get; }
    DbSet<IssueHistory> IssueHistories { get; }
    DbSet<IssueAssignment> IssueAssignments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
