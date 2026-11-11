using SampaCuida.Domain.Enums;

namespace SampaCuida.Application.IssueReports;

public enum IssueReportOrder
{
    PRIORITY,     // fila de trabalho do gestor: maior prioridade primeiro
    OPENING_DATE  // mais recentes primeiro
}

public record IssueReportQuery(
    int Page = 1,
    int PageSize = 10,
    IssueStatus? Status = null,
    int? IssueTypeId = null,
    IssueCategory? Category = null,
    Severity? Severity = null,
    IssueReportSource? Source = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    IssueReportOrder OrderBy = IssueReportOrder.PRIORITY);

// Visão do gestor na lista. Localização, contagem, última vez vista e prioridade são calculadas.
public record IssueReportSummaryResponse(
    int Id,
    string Title,
    IssueStatus Status,
    Severity? Severity,
    IssueReportSource Source,
    int IssueTypeId,
    string IssueTypeName,
    IssueCategory Category,
    DateTime OpeningDate,
    DateTime? ResolutionDate,
    string? Street,
    string? Neighborhood,
    int? Level,
    StreetSide? Side,
    int DetectionCount,
    DateTime? LastSeenAt,
    int PriorityScore);

public record IssueHistoryResponse(
    IssueStatus? PreviousStatus, IssueStatus NewStatus, string? Notes, DateTime ChangeDate, string? UserId);

public record IssueAssignmentResponse(
    int Id, int TeamId, string TeamName, DateTime AssignmentDate, DateTime? ClosingDate, bool IsOpen);

public record IssueReportDetailResponse(
    IssueReportSummaryResponse Summary,
    string? Description,
    IReadOnlyList<IssueHistoryResponse> History,
    IReadOnlyList<IssueAssignmentResponse> Assignments);

// Pedido do gestor. ASSIGNED não entra aqui: atribuir é pela rota de atribuições.
public record ChangeIssueStatusRequest(IssueStatus? Status, string? Notes);

public record CreateAssignmentRequest(int? TeamId);

// ---- Rota pública (cidadão): sem id de usuário, observações internas, equipes ou prioridade. ----

public record PublicIssueReportQuery(
    int Page = 1,
    int PageSize = 10,
    IssueStatus? Status = null,
    IssueCategory? Category = null,
    string? Neighborhood = null);

public record PublicIssueReportResponse(
    int Id,
    string Title,
    string? Description,
    string IssueTypeName,
    IssueCategory Category,
    Severity? Severity,
    IssueStatus Status,
    DateTime OpeningDate,
    DateTime? ResolutionDate,
    string? Street,
    string? Neighborhood,
    int? Level,
    StreetSide? Side,
    int DetectionCount,
    DateTime? LastSeenAt);
