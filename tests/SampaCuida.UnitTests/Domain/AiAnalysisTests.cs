using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.UnitTests.Domain;

public class AiAnalysisTests
{
    private static readonly DateTime Analyzed = new(2026, 11, 10, 15, 2, 0, DateTimeKind.Utc);

    private static AiAnalysis CreateAnalysis(AnalysisStatus status = AnalysisStatus.COMPLETED) =>
        new(imageId: 1, "yolov8-pothole", status, Analyzed, modelVersion: "1.2.0", processingTimeMs: 840);

    [Fact]
    public void Constructor_creates_analysis_without_detections()
    {
        var analysis = CreateAnalysis();

        Assert.Equal("yolov8-pothole", analysis.AiModel);
        Assert.Equal(840, analysis.ProcessingTimeMs);
        Assert.Empty(analysis.Detections);
    }

    [Fact]
    public void Constructor_rejects_negative_processing_time()
    {
        Assert.Throws<DomainException>(() =>
            new AiAnalysis(1, "yolov8", AnalysisStatus.COMPLETED, Analyzed, processingTimeMs: -1));
    }

    [Fact]
    public void AddDetection_adds_detection_linked_to_the_analysis()
    {
        var analysis = CreateAnalysis();

        var detection = analysis.AddDetection(
            issueTypeId: 2, 0.91m, Severity.HIGH, "10, 20, 300, 150", "Buraco largo na faixa da direita");

        Assert.Single(analysis.Detections);
        Assert.Same(analysis, detection.AiAnalysis);
        Assert.Equal(0.91m, detection.ConfidenceScore);
        Assert.Equal("10,20,300,150", detection.DetectionCoordinates);
        Assert.Null(detection.IssueReport);
    }

    [Fact]
    public void AddDetection_is_refused_when_analysis_failed()
    {
        var analysis = CreateAnalysis(AnalysisStatus.FAILED);

        var exception = Assert.Throws<DomainException>(() => analysis.AddDetection(issueTypeId: 2));

        Assert.Contains("falha", exception.Message);
        Assert.Empty(analysis.Detections);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void AddDetection_rejects_confidence_out_of_range(double confidence)
    {
        var analysis = CreateAnalysis();

        Assert.Throws<DomainException>(() => analysis.AddDetection(2, (decimal)confidence));
    }

    [Theory]
    [InlineData("10,20,300")]
    [InlineData("10,20,0,150")]
    [InlineData("-1,20,300,150")]
    [InlineData("a,b,c,d")]
    public void AddDetection_rejects_malformed_bounding_box(string coordinates)
    {
        var analysis = CreateAnalysis();

        var exception = Assert.Throws<DomainException>(() =>
            analysis.AddDetection(2, detectionCoordinates: coordinates));

        Assert.Contains("x,y,largura,altura", exception.Message);
    }
}
