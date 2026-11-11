using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;

namespace SampaCuida.Application.Analyses;

/// <summary>
/// Uma detecção da IA. level (altura na rua) e side (lado) são opcionais:
/// sem level, vale o número do ponto monitorado da câmera.
/// </summary>
public record DetectionRequest(
    int? IssueTypeId,
    decimal? ConfidenceScore,
    Severity? EstimatedSeverity,
    string? DetectionCoordinates,
    string? Description,
    int? Level,
    StreetSide? Side);

public record CreateAnalysisRequest(
    int? ImageId,
    string AiModel,
    string? ModelVersion,
    AnalysisStatus? Status,
    DateTimeOffset? AnalysisDate,
    int? ProcessingTimeMs,
    IReadOnlyList<DetectionRequest>? Detections);

// O que aconteceu com cada detecção: é assim que se vê a deduplicação acontecendo.
public enum DetectionOutcome
{
    OPENED_NEW_ISSUE,
    LINKED_TO_EXISTING_ISSUE,
    BELOW_CONFIDENCE_THRESHOLD
}

public record DetectionResultResponse(
    int DetectionId,
    int IssueTypeId,
    decimal? ConfidenceScore,
    DetectionOutcome Outcome,
    int? IssueReportId);

public record AnalysisResultResponse(
    int AnalysisId,
    int ImageId,
    int CaptureId,
    CaptureStatus CaptureStatus,
    AnalysisStatus Status,
    IReadOnlyList<DetectionResultResponse> Detections);

public record DetectionResponse(
    int Id,
    int IssueTypeId,
    decimal? ConfidenceScore,
    Severity? EstimatedSeverity,
    string? DetectionCoordinates,
    string? Description,
    int? IssueReportId)
{
    public static DetectionResponse FromEntity(AiDetection detection) => new(
        detection.Id, detection.IssueTypeId, detection.ConfidenceScore, detection.EstimatedSeverity,
        detection.DetectionCoordinates, detection.Description, detection.IssueReportId);
}

public record AnalysisResponse(
    int Id,
    int ImageId,
    string AiModel,
    string? ModelVersion,
    AnalysisStatus Status,
    DateTime AnalysisDate,
    int? ProcessingTimeMs,
    IReadOnlyList<DetectionResponse> Detections)
{
    public static AnalysisResponse FromEntity(AiAnalysis analysis) => new(
        analysis.Id, analysis.ImageId, analysis.AiModel, analysis.ModelVersion, analysis.Status,
        analysis.AnalysisDate, analysis.ProcessingTimeMs,
        analysis.Detections.Select(DetectionResponse.FromEntity).ToList());
}

/// <summary>Configuração "Detection" do appsettings.</summary>
public class DetectionOptions
{
    public const string SectionName = "Detection";

    // Abaixo desta confiança, a detecção é guardada mas não abre ocorrência.
    public decimal MinConfidence { get; set; } = 0.5m;
}
