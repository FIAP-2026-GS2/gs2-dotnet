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

    // Mensagem amigável para cada FK, quando o registro ainda tem dependentes.
    private static readonly Dictionary<string, string> ForeignKeyMessages = new()
    {
        ["FK_CAMERA_MONITORING_POINT_ID_POINT"] = "O ponto monitorado ainda tem câmeras vinculadas.",
        ["FK_TEAM_MUNICIPAL_AGENCY_ID_AGENCY"] = "O órgão ainda tem equipes vinculadas.",
        ["FK_CAPTURE_SCHEDULE_CAMERA_ID_CAMERA"] = "A câmera ainda tem agendas de captura (desative-as em vez de remover).",
        ["FK_CAPTURE_CAMERA_ID_CAMERA"] = "A câmera já tem capturas registradas (desative-a em vez de remover).",
        ["FK_CAPTURE_CAPTURE_SCHEDULE_ID_SCHEDULE"] = "A agenda já gerou capturas (desative-a em vez de remover).",
        ["FK_CAPTURE_TRIGGER_EVENT_ID_EVENT"] = "O alerta já gerou capturas.",
        ["FK_ISSUE_ASSIGNMENT_TEAM_ID_TEAM"] = "A equipe já tem atribuições (desative-a em vez de remover).",
        ["FK_AI_DETECTION_ISSUE_TYPE_ID_ISSUE_TYPE"] = "O tipo de problema já é usado por detecções (desative-o em vez de remover).",
        ["FK_ISSUE_REPORT_ISSUE_TYPE_ID_ISSUE_TYPE"] = "O tipo de problema já é usado por ocorrências (desative-o em vez de remover)."
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

    // O SQL Server (datetime2) não guarda se a data é UTC. Como gravamos sempre em UTC,
    // toda data lida volta marcada como UTC: as regras do Domain e o JSON ("...Z") ficam certos.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

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
            {
                var message = ForeignKeyMessages
                    .FirstOrDefault(entry => sql.Message.Contains(entry.Key)).Value
                    ?? "Operação recusada: o registro está ligado a outros dados.";

                throw new ConflictException(message);
            }

            throw;
        }
    }
}
