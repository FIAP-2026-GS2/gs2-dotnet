using SampaCuida.Domain.Common;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Domain.Entities;

/// <summary>
/// Ocorrência de zeladoria. É dona do histórico e das atribuições (agregado):
/// toda mudança de status passa por aqui e gera uma linha de IssueHistory.
/// Quantidade de detecções, última vez vista e prioridade são calculadas por consulta.
/// </summary>
public class IssueReport
{
    public const int TitleMaxLength = 150;
    public const int DescriptionMaxLength = 1000;
    public const int UserIdMaxLength = 100;

    // Transições permitidas. RESOLVED e REJECTED são finais: se o problema voltar,
    // a nova detecção abre outra ocorrência (a deduplicação só procura as abertas).
    // ASSIGNED não está aqui de propósito: só se chega a ele por AssignTo.
    private static readonly Dictionary<IssueStatus, IssueStatus[]> AllowedTransitions = new()
    {
        [IssueStatus.OPEN] = [IssueStatus.REJECTED],
        [IssueStatus.ASSIGNED] = [IssueStatus.IN_PROGRESS, IssueStatus.REJECTED],
        [IssueStatus.IN_PROGRESS] = [IssueStatus.RESOLVED],
        [IssueStatus.RESOLVED] = [],
        [IssueStatus.REJECTED] = []
    };

    private readonly List<IssueHistory> _history = [];
    private readonly List<IssueAssignment> _assignments = [];

    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public IssueReportSource Source { get; private set; }
    public Severity? Severity { get; private set; }
    public IssueStatus Status { get; private set; }
    public DateTime OpeningDate { get; private set; }
    public DateTime? ResolutionDate { get; private set; }

    // Altura na rua pelo número do imóvel (numeração métrica: 1500 ≈ 1.500 m do início).
    public int? Level { get; private set; }
    public StreetSide? Side { get; private set; }

    public int IssueTypeId { get; private set; }
    public IssueType? IssueType { get; private set; }

    // Usuário é da API Java (outro banco): guardamos só o id do token, sem FK.
    public string? UserId { get; private set; }

    public IReadOnlyCollection<IssueHistory> History => _history;
    public IReadOnlyCollection<IssueAssignment> Assignments => _assignments;

    public bool IsOpen => Status is not (IssueStatus.RESOLVED or IssueStatus.REJECTED);

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private IssueReport() { }

    private IssueReport(
        IssueReportSource source,
        int issueTypeId,
        string title,
        DateTime openingDate,
        string? description,
        int? level,
        StreetSide? side,
        string? userId)
    {
        if (level < 0)
            throw new DomainException("A altura na rua (número) não pode ser negativa.");

        Source = source;
        IssueTypeId = Guard.RequiredId(issueTypeId, "tipo de problema");
        Title = Guard.RequiredText(title, "título", TitleMaxLength);
        OpeningDate = Guard.Utc(openingDate, "abertura");
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
        Level = level;
        Side = side is null ? null : Guard.DefinedEnum(side.Value, "lado da rua");
        UserId = Guard.OptionalText(userId, "usuário", UserIdMaxLength);
        Status = IssueStatus.OPEN;

        _history.Add(new IssueHistory(this, null, IssueStatus.OPEN, "Ocorrência aberta.", openingDate, UserId));
    }

    public static IssueReport FromDetection(
        AiDetection detection,
        string title,
        DateTime openingDate,
        int? level = null,
        StreetSide? side = null,
        string? description = null)
    {
        var report = new IssueReport(
            IssueReportSource.AI_DETECTION, detection.IssueTypeId, title, openingDate,
            description ?? detection.Description, level, side, userId: null);

        report.RegisterDetection(detection);
        return report;
    }

    public static IssueReport FromCitizen(
        int issueTypeId,
        string userId,
        string title,
        DateTime openingDate,
        int? level = null,
        StreetSide? side = null,
        string? description = null)
    {
        Guard.RequiredText(userId, "usuário", UserIdMaxLength);

        return new IssueReport(
            IssueReportSource.CITIZEN, issueTypeId, title, openingDate, description, level, side, userId);
    }

    // Deduplicação: a nova detecção do mesmo problema entra nesta ocorrência,
    // em vez de abrir outra. A severidade pode subir.
    public void RegisterDetection(AiDetection detection)
    {
        if (!IsOpen)
            throw new DomainException("Só é possível vincular detecções a uma ocorrência aberta.");

        if (detection.IssueTypeId != IssueTypeId)
            throw new DomainException("A detecção é de outro tipo de problema.");

        detection.LinkTo(this);
        RaiseSeverity(detection.EstimatedSeverity);
    }

    // Severidade só sobe: um valor menor (ou nulo) é ignorado.
    public void RaiseSeverity(Severity? severity)
    {
        if (severity is not null && (Severity is null || severity > Severity))
            Severity = severity;
    }

    public IssueAssignment AssignTo(int teamId, DateTime assignedAt, string? userId = null)
    {
        if (Status != IssueStatus.OPEN)
            throw new DomainException($"Só é possível atribuir uma ocorrência com status OPEN (atual: {Status}).");

        if (_assignments.Any(assignment => assignment.IsOpen))
            throw new DomainException("A ocorrência já tem uma equipe atribuída.");

        var assignment = new IssueAssignment(this, teamId, assignedAt);
        _assignments.Add(assignment);

        RecordStatusChange(IssueStatus.ASSIGNED, assignedAt, "Equipe atribuída.", userId);
        return assignment;
    }

    public void ChangeStatus(IssueStatus newStatus, DateTime changedAt, string? notes = null, string? userId = null)
    {
        if (newStatus == IssueStatus.ASSIGNED)
            throw new DomainException("Para atribuir uma equipe, use a atribuição.");

        if (!AllowedTransitions[Status].Contains(newStatus))
            throw new DomainException($"Não é permitido mudar o status de {Status} para {newStatus}.");

        Guard.Utc(changedAt, "mudança");

        if (newStatus is IssueStatus.RESOLVED or IssueStatus.REJECTED)
        {
            // Encerrar a ocorrência fecha a atribuição aberta, se houver.
            _assignments.SingleOrDefault(assignment => assignment.IsOpen)?.Close(changedAt);

            if (newStatus == IssueStatus.RESOLVED)
                ResolutionDate = changedAt;
        }

        RecordStatusChange(newStatus, changedAt, notes, userId);
    }

    private void RecordStatusChange(IssueStatus newStatus, DateTime changedAt, string? notes, string? userId)
    {
        var previous = Status;
        Status = newStatus;
        _history.Add(new IssueHistory(this, previous, newStatus, notes, changedAt, userId));
    }
}
