using Microsoft.EntityFrameworkCore;
using SampaCuida.Application.Abstractions;
using SampaCuida.Domain.Common;
using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;

namespace SampaCuida.Application.IssueReports;

/// <summary>
/// Monta a visão das ocorrências com os indicadores calculados, usada pelo gestor e pela rota pública.
/// Os indicadores não são guardados (seguindo o DER): saem das detecções ligadas a cada ocorrência,
/// pelo caminho detecção → análise → imagem → captura → câmera → ponto monitorado.
/// </summary>
public class IssueReportReader(IAppDbContext db)
{
    public async Task<List<IssueReportSummaryResponse>> LoadAsync(
        IQueryable<IssueReport> reports, IssueCategory? category, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var rows = await (
                from report in reports
                join issueType in db.IssueTypes on report.IssueTypeId equals issueType.Id
                where category == null || issueType.Category == category
                select new
                {
                    report.Id, report.Title, report.Status, report.Severity, report.Source, report.IssueTypeId,
                    IssueTypeName = issueType.Name, issueType.Category, report.OpeningDate, report.ResolutionDate,
                    report.Level, report.Side
                })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return [];

        var ids = rows.Select(row => row.Id).ToList();

        // Uma consulta para as detecções de todas as ocorrências (e não uma por ocorrência).
        // Câmera e ponto são "left join": foto de cidadão não tem câmera.
        var detections = await (
                from detection in db.AiDetections
                where detection.IssueReportId != null && ids.Contains(detection.IssueReportId.Value)
                join analysis in db.AiAnalyses on detection.AiAnalysisId equals analysis.Id
                join image in db.Images on analysis.ImageId equals image.Id
                join capture in db.Captures on image.CaptureId equals capture.Id
                join camera in db.Cameras on capture.CameraId equals (int?)camera.Id into cameras
                from camera in cameras.DefaultIfEmpty()
                join point in db.MonitoringPoints on camera.MonitoringPointId equals point.Id into points
                from point in points.DefaultIfEmpty()
                select new
                {
                    ReportId = detection.IssueReportId!.Value,
                    capture.CaptureDate,
                    Street = point == null ? null : point.Street,
                    Neighborhood = point == null ? null : point.Neighborhood
                })
            .ToListAsync(cancellationToken);

        var byReport = detections.ToLookup(detection => detection.ReportId);

        return rows.Select(row =>
        {
            var seen = byReport[row.Id].OrderByDescending(detection => detection.CaptureDate).ToList();
            var latest = seen.FirstOrDefault();

            return new IssueReportSummaryResponse(
                row.Id, row.Title, row.Status, row.Severity, row.Source, row.IssueTypeId, row.IssueTypeName,
                row.Category, row.OpeningDate, row.ResolutionDate,
                latest?.Street, latest?.Neighborhood, row.Level, row.Side,
                DetectionCount: seen.Count,
                LastSeenAt: latest?.CaptureDate,
                PriorityScore: IssuePriority.Calculate(row.Severity, seen.Count, row.OpeningDate, nowUtc));
        }).ToList();
    }
}
