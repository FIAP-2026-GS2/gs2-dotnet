using Microsoft.Extensions.DependencyInjection;
using SampaCuida.Application;
using SampaCuida.Application.Analyses;
using SampaCuida.Domain.Enums;

namespace SampaCuida.UnitTests.Application;

public class AnalysisValidatorTests
{
    // Aplica a mesma configuração da API (mensagens em português e chaves em camelCase).
    static AnalysisValidatorTests() => new ServiceCollection().AddApplication();

    private static readonly DetectionRequest Pothole = new(
        IssueTypeId: 4, ConfidenceScore: 0.91m, EstimatedSeverity: Severity.HIGH,
        DetectionCoordinates: "10,20,300,150", Description: "Buraco largo", Level: 1500, Side: StreetSide.ODD);

    private static CreateAnalysisRequest Analysis(
        AnalysisStatus status = AnalysisStatus.COMPLETED, params DetectionRequest[] detections) => new(
        ImageId: 1, AiModel: "yolov8-zeladoria", ModelVersion: "1.0.0", Status: status,
        AnalysisDate: DateTimeOffset.Parse("2026-11-11T12:02:00-03:00"), ProcessingTimeMs: 840,
        Detections: detections);

    private static IDictionary<string, string[]> Errors(CreateAnalysisRequest request) =>
        new CreateAnalysisRequestValidator().Validate(request).ToDictionary();

    [Fact]
    public void Valid_analysis_with_detection_passes()
    {
        Assert.Empty(Errors(Analysis(detections: Pothole)));
    }

    [Fact]
    public void Failed_analysis_cannot_have_detections()
    {
        Assert.Contains("detections", Errors(Analysis(AnalysisStatus.FAILED, Pothole)).Keys);
    }

    [Fact]
    public void Failed_analysis_without_detections_passes()
    {
        Assert.Empty(Errors(Analysis(AnalysisStatus.FAILED)));
    }

    [Fact]
    public void Detection_requires_type_and_confidence_between_0_and_1()
    {
        var invalid = Pothole with { IssueTypeId = null, ConfidenceScore = 1.5m };

        var errors = Errors(Analysis(detections: invalid));

        Assert.Contains("detections[0].issueTypeId", errors.Keys);
        Assert.Contains("detections[0].confidenceScore", errors.Keys);
    }

    [Fact]
    public void Detection_rejects_negative_level()
    {
        Assert.Contains("detections[0].level", Errors(Analysis(detections: Pothole with { Level = -1 })).Keys);
    }
}
