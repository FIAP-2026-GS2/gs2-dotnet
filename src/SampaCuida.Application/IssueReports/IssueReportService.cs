using Microsoft.EntityFrameworkCore;
using SampaCuida.Application.Abstractions;
using SampaCuida.Application.Auth;
using SampaCuida.Application.Common.Exceptions;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Application.IssueReports;

public interface IIssueReportService
{
    Task<PagedResponse<IssueReportSummaryResponse>> ListAsync(IssueReportQuery query, CancellationToken cancellationToken);
    Task<IssueReportDetailResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IssueReportDetailResponse> ChangeStatusAsync(int id, ChangeIssueStatusRequest request, CancellationToken cancellationToken);
    Task<IssueReportDetailResponse> AssignAsync(int id, CreateAssignmentRequest request, CancellationToken cancellationToken);
    Task<PagedResponse<PublicIssueReportResponse>> ListPublicAsync(PublicIssueReportQuery query, CancellationToken cancellationToken);
}

public class IssueReportService(IAppDbContext db, TimeProvider timeProvider, ICurrentUser currentUser)
    : IIssueReportService
{
    private readonly IssueReportReader _reader = new(db);

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;

    // A prioridade é calculada, então a ordenação e a paginação acontecem em memória.
    // No MVP o volume é pequeno; em grande escala, a prioridade passaria a ser guardada.
    public async Task<PagedResponse<IssueReportSummaryResponse>> ListAsync(
        IssueReportQuery query, CancellationToken cancellationToken)
    {
        var reports = db.IssueReports.AsNoTracking();

        if (query.Status is not null)
            reports = reports.Where(report => report.Status == query.Status);

        if (query.IssueTypeId is not null)
            reports = reports.Where(report => report.IssueTypeId == query.IssueTypeId);

        if (query.Severity is not null)
            reports = reports.Where(report => report.Severity == query.Severity);

        if (query.Source is not null)
            reports = reports.Where(report => report.Source == query.Source);

        if (query.From is not null)
            reports = reports.Where(report => report.OpeningDate >= query.From.Value.UtcDateTime);

        if (query.To is not null)
            reports = reports.Where(report => report.OpeningDate <= query.To.Value.UtcDateTime);

        var items = await _reader.LoadAsync(reports, query.Category, Now(), cancellationToken);

        var ordered = query.OrderBy == IssueReportOrder.OPENING_DATE
            ? items.OrderByDescending(item => item.OpeningDate).ThenByDescending(item => item.Id)
            : items.OrderByDescending(item => item.PriorityScore).ThenBy(item => item.OpeningDate);

        return Page(ordered, query.Page, query.PageSize);
    }

    public async Task<IssueReportDetailResponse> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await DetailAsync(id, cancellationToken);

    // As regras de transição (e o histórico automático) ficam no Domain.
    public async Task<IssueReportDetailResponse> ChangeStatusAsync(
        int id, ChangeIssueStatusRequest request, CancellationToken cancellationToken)
    {
        var report = await LoadAggregateAsync(id, cancellationToken);

        // O usuário do token fica registrado no histórico (auditoria: quem mudou o status).
        report.ChangeStatus(request.Status!.Value, Now(), request.Notes, currentUser.UserId);

        await db.SaveChangesAsync(cancellationToken);
        return await DetailAsync(id, cancellationToken);
    }

    // OPEN: atribui. ASSIGNED: troca a equipe. Outros status: o Domain recusa.
    public async Task<IssueReportDetailResponse> AssignAsync(
        int id, CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var report = await LoadAggregateAsync(id, cancellationToken);
        var teamId = request.TeamId!.Value;

        var team = await db.Teams.AsNoTracking().FirstOrDefaultAsync(team => team.Id == teamId, cancellationToken)
                   ?? throw new DomainException($"A equipe {teamId} não existe.");

        if (!team.IsActive)
            throw new DomainException($"A equipe {teamId} está desativada.");

        if (report.Status == IssueStatus.ASSIGNED)
            report.Reassign(teamId, Now(), currentUser.UserId);
        else
            report.AssignTo(teamId, Now(), currentUser.UserId);

        await db.SaveChangesAsync(cancellationToken);
        return await DetailAsync(id, cancellationToken);
    }

    public async Task<PagedResponse<PublicIssueReportResponse>> ListPublicAsync(
        PublicIssueReportQuery query, CancellationToken cancellationToken)
    {
        // Falsos positivos rejeitados pela prefeitura nunca aparecem para o cidadão.
        var reports = db.IssueReports.AsNoTracking().Where(report => report.Status != IssueStatus.REJECTED);

        if (query.Status is not null)
            reports = reports.Where(report => report.Status == query.Status);

        var items = await _reader.LoadAsync(reports, query.Category, Now(), cancellationToken);

        // O bairro vem do ponto monitorado (calculado), por isso o filtro é feito depois.
        if (!string.IsNullOrWhiteSpace(query.Neighborhood))
            items = items
                .Where(item => string.Equals(item.Neighborhood, query.Neighborhood.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

        var ordered = items
            .OrderByDescending(item => item.LastSeenAt ?? item.OpeningDate)
            .Select(item => new PublicIssueReportResponse(
                item.Id, item.Title, Description: null, item.IssueTypeName, item.Category, item.Severity,
                item.Status, item.OpeningDate, item.ResolutionDate, item.Street, item.Neighborhood,
                item.Level, item.Side, item.DetectionCount, item.LastSeenAt));

        var page = Page(ordered, query.Page, query.PageSize);

        // A descrição (texto gerado pela IA) é buscada só para os itens da página.
        var ids = page.Items.Select(item => item.Id).ToList();
        var descriptions = await db.IssueReports.AsNoTracking()
            .Where(report => ids.Contains(report.Id))
            .ToDictionaryAsync(report => report.Id, report => report.Description, cancellationToken);

        return page with
        {
            Items = page.Items.Select(item => item with { Description = descriptions[item.Id] }).ToList()
        };
    }

    private async Task<IssueReport> LoadAggregateAsync(int id, CancellationToken cancellationToken) =>
        await db.IssueReports
            .Include(report => report.History)
            .Include(report => report.Assignments)
            .FirstOrDefaultAsync(report => report.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Ocorrência {id} não encontrada.");

    private async Task<IssueReportDetailResponse> DetailAsync(int id, CancellationToken cancellationToken)
    {
        var report = await db.IssueReports.AsNoTracking()
            .Include(report => report.History)
            .Include(report => report.Assignments)
            .FirstOrDefaultAsync(report => report.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Ocorrência {id} não encontrada.");

        var summary = (await _reader.LoadAsync(
            db.IssueReports.AsNoTracking().Where(item => item.Id == id), null, Now(), cancellationToken)).Single();

        var teamIds = report.Assignments.Select(assignment => assignment.TeamId).Distinct().ToList();
        var teamNames = await db.Teams.AsNoTracking()
            .Where(team => teamIds.Contains(team.Id))
            .ToDictionaryAsync(team => team.Id, team => team.Name, cancellationToken);

        return new IssueReportDetailResponse(
            summary,
            report.Description,
            report.History
                .OrderBy(history => history.ChangeDate).ThenBy(history => history.Id)
                .Select(history => new IssueHistoryResponse(
                    history.PreviousStatus, history.NewStatus, history.Notes, history.ChangeDate, history.UserId))
                .ToList(),
            report.Assignments
                .OrderBy(assignment => assignment.AssignmentDate).ThenBy(assignment => assignment.Id)
                .Select(assignment => new IssueAssignmentResponse(
                    assignment.Id, assignment.TeamId, teamNames.GetValueOrDefault(assignment.TeamId, "?"),
                    assignment.AssignmentDate, assignment.ClosingDate, assignment.IsOpen))
                .ToList());
    }

    private static PagedResponse<T> Page<T>(IEnumerable<T> items, int page, int pageSize)
    {
        var list = items.ToList();
        return new PagedResponse<T>(
            list.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, list.Count);
    }
}
