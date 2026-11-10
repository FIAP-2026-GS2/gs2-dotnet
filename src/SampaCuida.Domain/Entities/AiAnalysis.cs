using SampaCuida.Domain.Common;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Domain.Entities;

/// <summary>
/// Uma execução do modelo de IA sobre uma imagem. É a dona das detecções (agregado):
/// a análise e as suas detecções nascem e são gravadas juntas, como uma nota fiscal
/// e os seus itens. Detecção só é criada por AddDetection.
/// </summary>
public class AiAnalysis
{
    public const int AiModelMaxLength = 100;
    public const int ModelVersionMaxLength = 50;

    private readonly List<AiDetection> _detections = [];

    public int Id { get; private set; }
    public int ImageId { get; private set; }
    public Image? Image { get; private set; }
    public string AiModel { get; private set; } = null!;
    public string? ModelVersion { get; private set; }
    public AnalysisStatus Status { get; private set; }
    public DateTime AnalysisDate { get; private set; }
    public int? ProcessingTimeMs { get; private set; }

    public IReadOnlyCollection<AiDetection> Detections => _detections;

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private AiAnalysis() { }

    public AiAnalysis(
        int imageId,
        string aiModel,
        AnalysisStatus status,
        DateTime analysisDate,
        string? modelVersion = null,
        int? processingTimeMs = null)
    {
        if (processingTimeMs < 0)
            throw new DomainException("O tempo de processamento não pode ser negativo.");

        ImageId = Guard.RequiredId(imageId, "imagem");
        AiModel = Guard.RequiredText(aiModel, "modelo de IA", AiModelMaxLength);
        Status = Guard.DefinedEnum(status, "status da análise");
        AnalysisDate = Guard.Utc(analysisDate, "análise");
        ModelVersion = Guard.OptionalText(modelVersion, "versão do modelo", ModelVersionMaxLength);
        ProcessingTimeMs = processingTimeMs;
    }

    public AiDetection AddDetection(
        int issueTypeId,
        decimal? confidenceScore = null,
        Severity? estimatedSeverity = null,
        string? detectionCoordinates = null,
        string? description = null)
    {
        if (Status == AnalysisStatus.FAILED)
            throw new DomainException("Uma análise com falha não pode ter detecções.");

        var detection = new AiDetection(
            this, issueTypeId, confidenceScore, estimatedSeverity, detectionCoordinates, description);

        _detections.Add(detection);
        return detection;
    }
}
