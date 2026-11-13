using FluentValidation;
using SampaTech.Application.Common.Paging;
using SampaTech.Domain.Entities;

namespace SampaTech.Application.MonitoringPoints;

// Valida o formato do pedido e devolve todos os erros de uma vez.
// As regras de negócio continuam no Domain.
public class MonitoringPointRequestValidator : AbstractValidator<MonitoringPointRequest>
{
    public MonitoringPointRequestValidator()
    {
        RuleFor(x => x.Street).NotEmpty().MaximumLength(MonitoringPoint.StreetMaxLength).WithName("rua");
        RuleFor(x => x.City).NotEmpty().MaximumLength(MonitoringPoint.CityMaxLength).WithName("cidade");
        RuleFor(x => x.State).NotEmpty().Matches("^[A-Za-z]{2}$")
            .WithMessage("O estado deve ser a sigla da UF com 2 letras (ex.: SP).");
        RuleFor(x => x.Latitude).NotNull().InclusiveBetween(-90, 90).WithName("latitude");
        RuleFor(x => x.Longitude).NotNull().InclusiveBetween(-180, 180).WithName("longitude");
        RuleFor(x => x.StreetNumber).MaximumLength(MonitoringPoint.StreetNumberMaxLength).WithName("número");
        RuleFor(x => x.Neighborhood).MaximumLength(MonitoringPoint.NeighborhoodMaxLength).WithName("bairro");
        RuleFor(x => x.Description).MaximumLength(MonitoringPoint.DescriptionMaxLength).WithName("descrição");
    }
}

public class MonitoringPointQueryValidator : AbstractValidator<MonitoringPointQuery>
{
    public MonitoringPointQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingExtensions.MaxPageSize).WithName("tamanho da página");
    }
}
