using SampaTech.Domain.Common;
using SampaTech.Domain.Enums;
using SampaTech.Domain.Exceptions;

namespace SampaTech.Domain.Entities;

/// <summary>
/// Relato de um cidadão, com foto, sobre uma ocorrência resolvida: confirma o conserto ou
/// contesta ("o buraco continua lá"). A foto é uma captura de origem CITIZEN, já anonimizada.
/// O relato nasce PENDING e só passa a valer quando a equipe aprova (APPROVED) ou recusa (REJECTED).
/// </summary>
public class CitizenFeedback
{
    public const int NotesMaxLength = 500;

    public int Id { get; private set; }
    public int IssueReportId { get; private set; }

    public int CaptureId { get; private set; }
    public Capture? Capture { get; private set; }

    // Cidadão que enviou (id do token); sem FK, como nas demais tabelas.
    public string UserId { get; private set; } = null!;

    public CitizenFeedbackDecision Decision { get; private set; }
    public CitizenFeedbackStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewedBy { get; private set; }
    public string? ReviewNotes { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private CitizenFeedback() { }

    public CitizenFeedback(
        IssueReport report,
        Capture capture,
        string userId,
        CitizenFeedbackDecision decision,
        string? notes,
        DateTime createdAt)
    {
        if (report is null)
            throw new DomainException("A ocorrência é obrigatória.");

        // O cidadão confere o conserto: só faz sentido numa ocorrência resolvida.
        if (report.Status != IssueStatus.RESOLVED)
            throw new DomainException(
                $"Só é possível confirmar ou contestar uma ocorrência resolvida (status atual: {report.Status}).");

        Decision = Guard.DefinedEnum(decision, "decisão");

        // Contestar sem dizer o que viu não ajuda a equipe.
        if (Decision == CitizenFeedbackDecision.CONTEST && string.IsNullOrWhiteSpace(notes))
            throw new DomainException("Informe o que continua errado para contestar a ocorrência.");

        // A ocorrência já existe no banco (só uma resolvida chega aqui); a FK garante o vínculo.
        IssueReportId = report.Id;
        Capture = capture ?? throw new DomainException("A captura da foto é obrigatória.");
        UserId = Guard.RequiredText(userId, "usuário", IssueReport.UserIdMaxLength);
        Notes = Guard.OptionalText(notes, "observação", NotesMaxLength);
        CreatedAt = Guard.Utc(createdAt, "envio");
        Status = CitizenFeedbackStatus.PENDING;
    }

    public void Approve(DateTime reviewedAt, string? reviewedBy, string? notes) =>
        Review(CitizenFeedbackStatus.APPROVED, reviewedAt, reviewedBy, notes);

    // Recusar exige motivo: é o retorno que o cidadão recebe.
    public void Reject(DateTime reviewedAt, string? reviewedBy, string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            throw new DomainException("Informe o motivo da recusa.");

        Review(CitizenFeedbackStatus.REJECTED, reviewedAt, reviewedBy, notes);
    }

    private void Review(CitizenFeedbackStatus status, DateTime reviewedAt, string? reviewedBy, string? notes)
    {
        if (Status != CitizenFeedbackStatus.PENDING)
            throw new DomainException($"Este relato já foi revisado (status: {Status}).");

        Status = status;
        ReviewedAt = Guard.Utc(reviewedAt, "revisão");
        ReviewedBy = Guard.OptionalText(reviewedBy, "revisor", IssueReport.UserIdMaxLength);
        ReviewNotes = Guard.OptionalText(notes, "observação da revisão", NotesMaxLength);
    }
}
