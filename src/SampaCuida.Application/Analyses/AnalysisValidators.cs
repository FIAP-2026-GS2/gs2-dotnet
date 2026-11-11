using FluentValidation;
using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;

namespace SampaCuida.Application.Analyses;

public class DetectionRequestValidator : AbstractValidator<DetectionRequest>
{
    public DetectionRequestValidator()
    {
        RuleFor(x => x.IssueTypeId).NotNull().GreaterThan(0).WithName("tipo de problema");
        RuleFor(x => x.ConfidenceScore).NotNull().InclusiveBetween(0, 1).WithName("confiança");
        RuleFor(x => x.EstimatedSeverity).IsInEnum().WithName("severidade");
        RuleFor(x => x.DetectionCoordinates).MaximumLength(AiDetection.CoordinatesMaxLength).WithName("coordenadas");
        RuleFor(x => x.Description).MaximumLength(AiDetection.DescriptionMaxLength).WithName("descrição");
        RuleFor(x => x.Level).GreaterThanOrEqualTo(0).WithName("altura na rua");
        RuleFor(x => x.Side).IsInEnum().WithName("lado da rua");
    }
}

public class CreateAnalysisRequestValidator : AbstractValidator<CreateAnalysisRequest>
{
    public CreateAnalysisRequestValidator()
    {
        RuleFor(x => x.ImageId).NotNull().GreaterThan(0).WithName("imagem");
        RuleFor(x => x.AiModel).NotEmpty().MaximumLength(AiAnalysis.AiModelMaxLength).WithName("modelo de IA");
        RuleFor(x => x.ModelVersion).MaximumLength(AiAnalysis.ModelVersionMaxLength).WithName("versão do modelo");
        RuleFor(x => x.Status).NotNull().IsInEnum().WithName("status");
        RuleFor(x => x.AnalysisDate).NotNull().WithName("data da análise");
        RuleFor(x => x.ProcessingTimeMs).GreaterThanOrEqualTo(0).WithName("tempo de processamento");
        RuleFor(x => x.Detections).NotNull().WithName("detecções");
        RuleForEach(x => x.Detections).SetValidator(new DetectionRequestValidator());

        // Análise com falha não tem o que detectar.
        RuleFor(x => x.Detections).Empty()
            .When(x => x.Status == AnalysisStatus.FAILED)
            .WithMessage("Uma análise com falha não pode ter detecções.");
    }
}
