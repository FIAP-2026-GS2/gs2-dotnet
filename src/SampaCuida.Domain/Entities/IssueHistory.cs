using SampaCuida.Domain.Common;
using SampaCuida.Domain.Enums;

namespace SampaCuida.Domain.Entities;

/// <summary>Registro de uma mudança de status. Criado só pela própria ocorrência.</summary>
public class IssueHistory
{
    public const int NotesMaxLength = 500;

    public int Id { get; private set; }
    public int IssueReportId { get; private set; }
    public IssueReport? IssueReport { get; private set; }

    // Nulo na abertura: antes da abertura, a ocorrência não tinha status.
    public IssueStatus? PreviousStatus { get; private set; }
    public IssueStatus NewStatus { get; private set; }
    public string? Notes { get; private set; }
    public DateTime ChangeDate { get; private set; }

    // Quem mudou (id do token da API Java ou do gestor); sem FK.
    public string? UserId { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private IssueHistory() { }

    internal IssueHistory(
        IssueReport issueReport,
        IssueStatus? previousStatus,
        IssueStatus newStatus,
        string? notes,
        DateTime changeDate,
        string? userId)
    {
        IssueReport = issueReport;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Notes = Guard.OptionalText(notes, "observação", NotesMaxLength);
        ChangeDate = Guard.Utc(changeDate, "mudança");
        UserId = userId;
    }
}
