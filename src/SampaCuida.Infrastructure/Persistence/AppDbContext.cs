using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SampaCuida.Application.Abstractions;
using SampaCuida.Application.Common.Exceptions;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    // Códigos de erro do SQL Server.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
    private const int ForeignKeyViolation = 547;

    // Mensagem amigável para cada índice único conhecido.
    private static readonly Dictionary<string, string> UniqueIndexMessages = new()
    {
        ["IX_CAMERA_CAMERA_CODE"] = "Já existe uma câmera com esse código.",
        ["IX_ISSUE_TYPE_NAME"] = "Já existe um tipo de problema com esse nome.",
        ["UX_CAPTURE_CAMERA_DATE"] = "Essa câmera já tem uma captura nesse instante.",
        ["UX_ISSUE_ASSIGNMENT_OPEN"] = "A ocorrência já tem uma equipe atribuída."
    };

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

    // Traduz as recusas do banco (índice único e FK) em ConflictException, que a API devolve como 409.
    // Assim as rotas não precisam de try/catch e o Application não conhece o SQL Server.
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException sql)
        {
            if (sql.Number is UniqueIndexViolation or UniqueConstraintViolation)
            {
                var message = UniqueIndexMessages
                    .FirstOrDefault(entry => sql.Message.Contains(entry.Key)).Value
                    ?? "Já existe um registro com esses dados.";

                throw new ConflictException(message);
            }

            if (sql.Number == ForeignKeyViolation)
                throw new ConflictException(
                    "Operação recusada: o registro está ligado a outros dados (ex.: um ponto que ainda tem câmeras).");

            throw;
        }
    }
}
