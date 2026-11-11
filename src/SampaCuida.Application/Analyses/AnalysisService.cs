using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SampaCuida.Application.Abstractions;
using SampaCuida.Application.Common.Exceptions;
using SampaCuida.Domain.Common;
using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Application.Analyses;

public interface IAnalysisService
{
    Task<AnalysisResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<AnalysisResultResponse> CreateAsync(CreateAnalysisRequest request, CancellationToken cancellationToken);
}

public class AnalysisService(IAppDbContext db, IOptions<DetectionOptions> options) : IAnalysisService
{
    private readonly decimal _minConfidence = options.Value.MinConfidence;

    public async Task<AnalysisResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var analysis = await db.AiAnalyses.AsNoTracking()
            .Include(analysis => analysis.Detections)
            .FirstOrDefaultAsync(analysis => analysis.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Análise {id} não encontrada.");

        return AnalysisResponse.FromEntity(analysis);
    }

    /// <summary>
    /// Registra a análise e decide, para cada detecção, se abre uma ocorrência nova,
    /// se soma numa ocorrência aberta (deduplicação) ou se fica só registrada (confiança baixa).
    /// Tudo é gravado num único SaveChanges: ou grava tudo, ou nada.
    /// </summary>
    public async Task<AnalysisResultResponse> CreateAsync(
        CreateAnalysisRequest request, CancellationToken cancellationToken)
    {
        var image = await db.Images.AsNoTracking()
            .FirstOrDefaultAsync(image => image.Id == request.ImageId, cancellationToken)
            ?? throw new DomainException($"A imagem {request.ImageId} não existe.");

        var capture = await db.Captures.FirstAsync(capture => capture.Id == image.CaptureId, cancellationToken);

        // Terceira proteção contra duplicidade: cada captura é analisada uma vez só.
        if (capture.Status != CaptureStatus.PENDING)
            throw new DomainException($"A captura {capture.Id} já foi analisada (status: {capture.Status}).");

        var analysis = new AiAnalysis(
            image.Id, request.AiModel, request.Status!.Value, request.AnalysisDate!.Value.UtcDateTime,
            request.ModelVersion, request.ProcessingTimeMs);

        var results = new List<(AiDetection Detection, DetectionOutcome Outcome, IssueReport? Report)>();

        if (analysis.Status == AnalysisStatus.FAILED)
        {
            capture.MarkFailed();
        }
        else
        {
            await ProcessDetectionsAsync(request, capture, analysis, results, cancellationToken);
            capture.MarkAnalyzed();
        }

        db.AiAnalyses.Add(analysis);
        await db.SaveChangesAsync(cancellationToken);

        // Só depois de gravar os ids existem.
        return new AnalysisResultResponse(
            analysis.Id, image.Id, capture.Id, capture.Status, analysis.Status,
            results.Select(result => new DetectionResultResponse(
                result.Detection.Id, result.Detection.IssueTypeId, result.Detection.ConfidenceScore,
                result.Outcome, result.Report?.Id)).ToList());
    }

    private async Task ProcessDetectionsAsync(
        CreateAnalysisRequest request,
        Capture capture,
        AiAnalysis analysis,
        List<(AiDetection, DetectionOutcome, IssueReport?)> results,
        CancellationToken cancellationToken)
    {
        var issueTypes = await LoadActiveIssueTypesAsync(request.Detections!, cancellationToken);

        // Rua e número vêm do ponto monitorado da câmera (foto de cidadão não tem câmera).
        var point = capture.CameraId is null
            ? null
            : await (from camera in db.Cameras
                     join monitoringPoint in db.MonitoringPoints on camera.MonitoringPointId equals monitoringPoint.Id
                     where camera.Id == capture.CameraId
                     select new { monitoringPoint.Street, monitoringPoint.StreetNumber })
                .FirstAsync(cancellationToken);

        // Ocorrências abertas nesta mesma análise: dois buracos no mesmo frame não abrem duas.
        var openedHere = new List<(IssueReport Report, IssueLocation Location)>();
        var openingDate = analysis.AnalysisDate;

        foreach (var item in request.Detections!)
        {
            var detection = analysis.AddDetection(
                item.IssueTypeId!.Value, item.ConfidenceScore, item.EstimatedSeverity,
                item.DetectionCoordinates, item.Description);

            if (item.ConfidenceScore < _minConfidence)
            {
                results.Add((detection, DetectionOutcome.BELOW_CONFIDENCE_THRESHOLD, null));
                continue;
            }

            var location = new IssueLocation(
                point?.Street, item.Level ?? IssueLocation.LevelFromStreetNumber(point?.StreetNumber), item.Side);

            var report = openedHere
                             .Where(opened => opened.Report.IssueTypeId == detection.IssueTypeId)
                             .Where(opened => capture.CameraId is not null || opened.Location.IsSameSpot(location))
                             .Select(opened => opened.Report)
                             .FirstOrDefault()
                         ?? await FindOpenDuplicateAsync(detection.IssueTypeId, capture.CameraId, location, cancellationToken);

            if (report is not null)
            {
                report.RegisterDetection(detection);
                results.Add((detection, DetectionOutcome.LINKED_TO_EXISTING_ISSUE, report));
                continue;
            }

            report = IssueReport.FromDetection(
                detection, Title(issueTypes[detection.IssueTypeId].Name, location), openingDate,
                location.Level, location.Side);

            db.IssueReports.Add(report);
            openedHere.Add((report, location));
            results.Add((detection, DetectionOutcome.OPENED_NEW_ISSUE, report));
        }
    }

    /// <summary>
    /// Deduplicação: ocorrência aberta do mesmo tipo, vista pela mesma câmera ou no mesmo
    /// lugar (mesma rua, mesmo lado e até 30 de altura). A regra "mesmo lugar" fica no Domain
    /// (IssueLocation.IsSameSpot); aqui só buscamos as candidatas com a rua de cada uma.
    /// </summary>
    private async Task<IssueReport?> FindOpenDuplicateAsync(
        int issueTypeId, int? cameraId, IssueLocation location, CancellationToken cancellationToken)
    {
        // Foto de cidadão (sem câmera) abre sempre uma nova: a deduplicação por coordenadas é melhoria futura.
        if (cameraId is null)
            return null;

        var candidates = await (
                from detection in db.AiDetections
                join report in db.IssueReports on detection.IssueReportId equals (int?)report.Id
                join analysis in db.AiAnalyses on detection.AiAnalysisId equals analysis.Id
                join image in db.Images on analysis.ImageId equals image.Id
                join capture in db.Captures on image.CaptureId equals capture.Id
                join camera in db.Cameras on capture.CameraId equals (int?)camera.Id
                join point in db.MonitoringPoints on camera.MonitoringPointId equals point.Id
                where report.IssueTypeId == issueTypeId
                      && report.Status != IssueStatus.RESOLVED
                      && report.Status != IssueStatus.REJECTED
                select new { ReportId = report.Id, report.OpeningDate, CameraId = camera.Id, point.Street, report.Level, report.Side })
            .Distinct()
            .ToListAsync(cancellationToken);

        var match = candidates
            .Where(candidate => candidate.CameraId == cameraId
                                || new IssueLocation(candidate.Street, candidate.Level, candidate.Side).IsSameSpot(location))
            .OrderBy(candidate => candidate.OpeningDate)
            .FirstOrDefault();

        return match is null
            ? null
            : await db.IssueReports.FirstAsync(report => report.Id == match.ReportId, cancellationToken);
    }

    private async Task<Dictionary<int, IssueType>> LoadActiveIssueTypesAsync(
        IEnumerable<DetectionRequest> detections, CancellationToken cancellationToken)
    {
        var ids = detections.Select(detection => detection.IssueTypeId!.Value).Distinct().ToList();

        var issueTypes = await db.IssueTypes.AsNoTracking()
            .Where(issueType => ids.Contains(issueType.Id))
            .ToDictionaryAsync(issueType => issueType.Id, cancellationToken);

        foreach (var id in ids)
        {
            if (!issueTypes.TryGetValue(id, out var issueType))
                throw new DomainException($"O tipo de problema {id} não existe.");

            if (!issueType.IsActive)
                throw new DomainException($"O tipo de problema {id} está desativado.");
        }

        return issueTypes;
    }

    // Ex.: "Buraco no asfalto - Av. Paulista, 1500"
    private static string Title(string issueTypeName, IssueLocation location)
    {
        var place = location.Street is null
            ? null
            : location.Level is null ? location.Street : $"{location.Street}, {location.Level}";

        var title = place is null ? issueTypeName : $"{issueTypeName} - {place}";
        return title.Length <= IssueReport.TitleMaxLength ? title : title[..IssueReport.TitleMaxLength];
    }
}
