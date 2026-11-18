using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SampaTech.Application.Abstractions;
using SampaTech.Application.Auth;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Paging;
using SampaTech.Application.Common.Photos;
using SampaTech.Domain.Entities;
using SampaTech.Domain.Enums;
using SampaTech.Domain.Exceptions;

namespace SampaTech.Application.CitizenFeedbacks;

public interface ICitizenFeedbackService
{
    Task<CitizenFeedbackResponse> SubmitAsync(
        int issueReportId, SubmitCitizenFeedbackRequest request, PhotoUpload? photo, CancellationToken cancellationToken);
    Task<PagedResponse<CitizenFeedbackResponse>> ListAsync(CitizenFeedbackQuery query, CancellationToken cancellationToken);
    Task<CitizenFeedbackResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<StoredPhoto> GetPhotoAsync(int id, CancellationToken cancellationToken);
    Task<CitizenFeedbackResponse> ReviewAsync(int id, ReviewCitizenFeedbackRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Fluxo do cidadão: manda a foto de uma ocorrência resolvida (confirma ou contesta o conserto),
/// a IA borra rostos e placas, a foto vai para o Blob e o relato fica PENDING até a equipe revisar.
/// </summary>
public class CitizenFeedbackService(
    IAppDbContext db,
    IPhotoAnonymizer anonymizer,
    IPhotoStorage storage,
    IOptions<CitizenPhotoOptions> options,
    TimeProvider timeProvider,
    ICurrentUser currentUser) : ICitizenFeedbackService
{
    private const string PendingMessage =
        "Foto recebida e validada pela IA. Aguardamos a aprovação da equipe para confirmar o relato.";

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<CitizenFeedbackResponse> SubmitAsync(
        int issueReportId, SubmitCitizenFeedbackRequest request, PhotoUpload? photo, CancellationToken cancellationToken)
    {
        // 1. A foto: obrigatória, até o tamanho máximo e só JPEG ou PNG (conferido pelos bytes do arquivo).
        if (photo is null || photo.Content.Length == 0)
            throw new DomainException("Envie a foto (JPG ou PNG).");

        var maxBytes = options.Value.MaxBytes;
        if (photo.Content.Length > maxBytes)
            throw new PayloadTooLargeException($"A foto deve ter até {maxBytes / (1024 * 1024)} MB.");

        var contentType = PhotoFormat.Detect(photo.Content)
                          ?? throw new DomainException("A foto deve ser uma imagem JPG ou PNG.");

        // 2. A ocorrência precisa existir e estar resolvida (o cidadão confere o conserto).
        var report = await db.IssueReports.FirstOrDefaultAsync(item => item.Id == issueReportId, cancellationToken)
                     ?? throw new NotFoundException($"Ocorrência {issueReportId} não encontrada.");

        if (report.Status != IssueStatus.RESOLVED)
            throw new ConflictException(
                $"Só é possível confirmar ou contestar uma ocorrência resolvida (status atual: {report.Status}).");

        var userId = currentUser.UserId ?? throw new UnauthorizedException("Usuário não identificado no token.");

        // 3. Privacidade: só a versão borrada é guardada. Sem a IA, nada é gravado (503).
        var anonymized = await anonymizer.AnonymizeAsync(photo, contentType, cancellationToken);
        var storedType = PhotoFormat.Detect(anonymized)
                         ?? throw new ServiceUnavailableException("O serviço de IA devolveu um arquivo que não é imagem.");

        var now = Now();
        var fileName = $"issue-{issueReportId}/{Guid.NewGuid():N}.{PhotoFormat.Extension(storedType)}";
        var url = await storage.SaveAsync(fileName, storedType, anonymized, cancellationToken);

        // 4. Captura de cidadão + imagem + relato: um único SaveChanges, numa transação só.
        var capture = Capture.FromCitizen(userId, request.Latitude!.Value, request.Longitude!.Value, now);
        var image = new Image(capture, fileName, url, isAnonymized: true, now,
            Convert.ToHexString(SHA256.HashData(anonymized)).ToLowerInvariant());
        var feedback = new CitizenFeedback(report, capture, userId, request.Decision!.Value, request.Notes, now);

        db.Captures.Add(capture);
        db.Images.Add(image);
        db.CitizenFeedbacks.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(feedback, cancellationToken);
    }

    // Fila da equipe: por padrão, os mais antigos primeiro (quem esperou mais é revisado antes).
    public async Task<PagedResponse<CitizenFeedbackResponse>> ListAsync(
        CitizenFeedbackQuery query, CancellationToken cancellationToken)
    {
        var feedbacks = db.CitizenFeedbacks.AsNoTracking();

        if (query.Status is not null)
            feedbacks = feedbacks.Where(feedback => feedback.Status == query.Status);

        if (query.IssueReportId is not null)
            feedbacks = feedbacks.Where(feedback => feedback.IssueReportId == query.IssueReportId);

        var totalItems = await feedbacks.CountAsync(cancellationToken);
        var page = await feedbacks
            .OrderBy(feedback => feedback.CreatedAt).ThenBy(feedback => feedback.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = new List<CitizenFeedbackResponse>(page.Count);
        foreach (var feedback in page)
            items.Add(await ToResponseAsync(feedback, cancellationToken));

        return new PagedResponse<CitizenFeedbackResponse>(items, query.Page, query.PageSize, totalItems);
    }

    // O cidadão só enxerga os próprios relatos (o de outra pessoa responde 404, sem revelar que existe).
    public async Task<CitizenFeedbackResponse> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await ToResponseAsync(await FindVisibleAsync(id, cancellationToken), cancellationToken);

    // A foto passa pela API (com login e papéis): o container do Blob continua privado.
    public async Task<StoredPhoto> GetPhotoAsync(int id, CancellationToken cancellationToken)
    {
        var feedback = await FindVisibleAsync(id, cancellationToken);

        var fileName = await db.Images.AsNoTracking()
            .Where(image => image.CaptureId == feedback.CaptureId)
            .Select(image => image.FileName)
            .FirstOrDefaultAsync(cancellationToken);

        return (fileName is null ? null : await storage.ReadAsync(fileName, cancellationToken))
               ?? throw new NotFoundException($"A foto do relato {id} não foi encontrada.");
    }

    private async Task<CitizenFeedback> FindVisibleAsync(int id, CancellationToken cancellationToken)
    {
        var feedback = await db.CitizenFeedbacks.AsNoTracking()
                           .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        var isCitizen = currentUser.IsInRole(Roles.Citizen)
                        && !currentUser.IsInRole(Roles.Admin)
                        && !currentUser.IsInRole(Roles.Manager)
                        && !currentUser.IsInRole(Roles.Viewer);

        if (feedback is null || (isCitizen && feedback.UserId != currentUser.UserId))
            throw new NotFoundException($"Relato {id} não encontrado.");

        return feedback;
    }

    // Aprovar uma contestação reabre a ocorrência; aprovar uma confirmação registra no histórico.
    public async Task<CitizenFeedbackResponse> ReviewAsync(
        int id, ReviewCitizenFeedbackRequest request, CancellationToken cancellationToken)
    {
        var feedback = await db.CitizenFeedbacks.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
                       ?? throw new NotFoundException($"Relato {id} não encontrado.");

        if (feedback.Status != CitizenFeedbackStatus.PENDING)
            throw new ConflictException($"Este relato já foi revisado (status: {feedback.Status}).");

        var now = Now();
        var reviewer = currentUser.UserId;

        if (request.Approved == true)
        {
            feedback.Approve(now, reviewer, request.Notes);

            var report = await db.IssueReports
                .Include(item => item.History)
                .FirstAsync(item => item.Id == feedback.IssueReportId, cancellationToken);

            // Se outra contestação já reabriu a ocorrência, esta só fica aprovada.
            if (report.Status == IssueStatus.RESOLVED)
            {
                if (feedback.Decision == CitizenFeedbackDecision.CONTEST)
                    report.ReopenByCitizen(now,
                        $"Reaberta: cidadão contestou o conserto com foto (relato {feedback.Id}). {feedback.Notes}".Trim(),
                        reviewer);
                else
                    report.RecordCitizenConfirmation(now,
                        $"Conserto confirmado por cidadão com foto (relato {feedback.Id}).", reviewer);
            }
        }
        else
        {
            feedback.Reject(now, reviewer, request.Notes);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(feedback, cancellationToken);
    }

    private async Task<CitizenFeedbackResponse> ToResponseAsync(
        CitizenFeedback feedback, CancellationToken cancellationToken)
    {
        var photoUrl = await db.Images.AsNoTracking()
            .Where(image => image.CaptureId == feedback.CaptureId)
            .Select(image => image.FilePath)
            .FirstOrDefaultAsync(cancellationToken);

        var confirmations = await db.CitizenFeedbacks.AsNoTracking()
            .CountAsync(item => item.IssueReportId == feedback.IssueReportId
                                && item.Decision == CitizenFeedbackDecision.CONFIRM
                                && item.Status == CitizenFeedbackStatus.APPROVED, cancellationToken);

        var message = feedback.Status switch
        {
            CitizenFeedbackStatus.PENDING => PendingMessage,
            CitizenFeedbackStatus.APPROVED when feedback.Decision == CitizenFeedbackDecision.CONFIRM =>
                $"Ocorrência resolvida e validada por {confirmations} cidadão(s).",
            CitizenFeedbackStatus.APPROVED =>
                "Contestada e não resolvida: a ocorrência foi reaberta para a equipe.",
            _ => $"O relato não foi aprovado pela equipe: {feedback.ReviewNotes}"
        };

        return new CitizenFeedbackResponse(
            feedback.Id, feedback.IssueReportId, feedback.CaptureId, photoUrl, feedback.Decision, feedback.Status,
            feedback.Notes, feedback.CreatedAt, feedback.ReviewedAt, feedback.ReviewNotes, message);
    }
}
