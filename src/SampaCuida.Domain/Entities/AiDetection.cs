using SampaCuida.Domain.Common;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Domain.Entities;

/// <summary>Um problema encontrado pela IA numa imagem. Criada só por AiAnalysis.AddDetection.</summary>
public class AiDetection
{
    public const int CoordinatesMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    public int Id { get; private set; }
    public int AiAnalysisId { get; private set; }
    public AiAnalysis? AiAnalysis { get; private set; }
    public int IssueTypeId { get; private set; }
    public IssueType? IssueType { get; private set; }
    public decimal? ConfidenceScore { get; private set; }
    public Severity? EstimatedSeverity { get; private set; }

    // Retângulo do problema na imagem: "x,y,largura,altura" (pixels).
    public string? DetectionCoordinates { get; private set; }

    // Texto gerado pela IA generativa.
    public string? Description { get; private set; }

    public int? IssueReportId { get; private set; }
    public IssueReport? IssueReport { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private AiDetection() { }

    // internal: fora do Domain, a detecção só nasce pela análise (AddDetection).
    internal AiDetection(
        AiAnalysis aiAnalysis,
        int issueTypeId,
        decimal? confidenceScore,
        Severity? estimatedSeverity,
        string? detectionCoordinates,
        string? description)
    {
        if (confidenceScore is < 0 or > 1)
            throw new DomainException("A confiança deve estar entre 0 e 1.");

        AiAnalysis = aiAnalysis;
        IssueTypeId = Guard.RequiredId(issueTypeId, "tipo de problema");
        ConfidenceScore = confidenceScore;
        EstimatedSeverity = estimatedSeverity is null
            ? null
            : Guard.DefinedEnum(estimatedSeverity.Value, "severidade");
        DetectionCoordinates = BoundingBox(detectionCoordinates);
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
    }

    // Chamado pela ocorrência, ao abrir ou ao deduplicar.
    internal void LinkTo(IssueReport issueReport)
    {
        if (IssueReport is not null || IssueReportId is not null)
            throw new DomainException("A detecção já está vinculada a uma ocorrência.");

        IssueReport = issueReport;
    }

    private static string? BoundingBox(string? value)
    {
        var text = Guard.OptionalText(value, "coordenadas", CoordinatesMaxLength);
        if (text is null)
            return null;

        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        var numbers = parts.Select(part => int.TryParse(part, out var number) ? number : -1).ToArray();

        // x e y podem ser 0; largura e altura precisam ser positivas.
        if (numbers.Length != 4 || numbers[0] < 0 || numbers[1] < 0 || numbers[2] <= 0 || numbers[3] <= 0)
            throw new DomainException("As coordenadas devem estar no formato x,y,largura,altura (inteiros).");

        return string.Join(',', numbers);
    }
}
