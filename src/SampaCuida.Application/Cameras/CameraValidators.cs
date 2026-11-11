using FluentValidation;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Application.Cameras;

public class CreateCameraRequestValidator : AbstractValidator<CreateCameraRequest>
{
    public CreateCameraRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(Camera.CodeMaxLength).WithName("código");
        RuleFor(x => x.MonitoringPointId).GreaterThan(0).WithName("ponto monitorado");
        RuleFor(x => x.Description).MaximumLength(Camera.DescriptionMaxLength).WithName("descrição");
    }
}

public class UpdateCameraRequestValidator : AbstractValidator<UpdateCameraRequest>
{
    public UpdateCameraRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(Camera.CodeMaxLength).WithName("código");
        RuleFor(x => x.MonitoringPointId).GreaterThan(0).WithName("ponto monitorado");
        RuleFor(x => x.Description).MaximumLength(Camera.DescriptionMaxLength).WithName("descrição");
        RuleFor(x => x.Status).NotNull().IsInEnum().WithName("status");
    }
}

public class CameraQueryValidator : AbstractValidator<CameraQuery>
{
    public CameraQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingExtensions.MaxPageSize).WithName("tamanho da página");
    }
}
