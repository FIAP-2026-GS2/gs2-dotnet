using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SampaTech.Application.Abstractions;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Paging;
using SampaTech.Domain.Entities;

namespace SampaTech.Application.MunicipalAgencies;

public record MunicipalAgencyRequest(
    string Name,
    string City,
    string State,
    string? Description,
    string? Subprefecture);

public record MunicipalAgencyResponse(
    int Id,
    string Name,
    string? Description,
    string City,
    string State,
    string? Subprefecture,
    bool IsActive)
{
    public static MunicipalAgencyResponse FromEntity(MunicipalAgency agency) => new(
        agency.Id, agency.Name, agency.Description, agency.City, agency.State, agency.Subprefecture,
        agency.IsActive);
}

public record MunicipalAgencyQuery(int Page = 1, int PageSize = 10, bool? IsActive = null);

public class MunicipalAgencyRequestValidator : AbstractValidator<MunicipalAgencyRequest>
{
    public MunicipalAgencyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(MunicipalAgency.NameMaxLength).WithName("nome");
        RuleFor(x => x.City).NotEmpty().MaximumLength(MunicipalAgency.CityMaxLength).WithName("cidade");
        RuleFor(x => x.State).NotEmpty().Matches("^[A-Za-z]{2}$")
            .WithMessage("O estado deve ser a sigla da UF com 2 letras (ex.: SP).");
        RuleFor(x => x.Description).MaximumLength(MunicipalAgency.DescriptionMaxLength).WithName("descrição");
        RuleFor(x => x.Subprefecture).MaximumLength(MunicipalAgency.SubprefectureMaxLength).WithName("subprefeitura");
    }
}

public class MunicipalAgencyQueryValidator : AbstractValidator<MunicipalAgencyQuery>
{
    public MunicipalAgencyQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingExtensions.MaxPageSize).WithName("tamanho da página");
    }
}

public interface IMunicipalAgencyService
{
    Task<PagedResponse<MunicipalAgencyResponse>> ListAsync(MunicipalAgencyQuery query, CancellationToken cancellationToken);
    Task<MunicipalAgencyResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<MunicipalAgencyResponse> CreateAsync(MunicipalAgencyRequest request, CancellationToken cancellationToken);
    Task<MunicipalAgencyResponse> UpdateAsync(int id, MunicipalAgencyRequest request, CancellationToken cancellationToken);
    Task<MunicipalAgencyResponse> SetActiveAsync(int id, bool active, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

public class MunicipalAgencyService(IAppDbContext db) : IMunicipalAgencyService
{
    public async Task<PagedResponse<MunicipalAgencyResponse>> ListAsync(
        MunicipalAgencyQuery query, CancellationToken cancellationToken)
    {
        var agencies = db.MunicipalAgencies.AsNoTracking();

        if (query.IsActive is not null)
            agencies = agencies.Where(agency => agency.IsActive == query.IsActive);

        return await agencies
            .OrderBy(agency => agency.Id)
            .ToPagedAsync(query.Page, query.PageSize, MunicipalAgencyResponse.FromEntity, cancellationToken);
    }

    public async Task<MunicipalAgencyResponse> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        MunicipalAgencyResponse.FromEntity(await FindAsync(id, cancellationToken));

    public async Task<MunicipalAgencyResponse> CreateAsync(
        MunicipalAgencyRequest request, CancellationToken cancellationToken)
    {
        var agency = new MunicipalAgency(
            request.Name, request.City, request.State, request.Description, request.Subprefecture);

        db.MunicipalAgencies.Add(agency);
        await db.SaveChangesAsync(cancellationToken);

        return MunicipalAgencyResponse.FromEntity(agency);
    }

    public async Task<MunicipalAgencyResponse> UpdateAsync(
        int id, MunicipalAgencyRequest request, CancellationToken cancellationToken)
    {
        var agency = await FindAsync(id, cancellationToken);

        agency.Update(request.Name, request.City, request.State, request.Description, request.Subprefecture);

        await db.SaveChangesAsync(cancellationToken);
        return MunicipalAgencyResponse.FromEntity(agency);
    }

    public async Task<MunicipalAgencyResponse> SetActiveAsync(int id, bool active, CancellationToken cancellationToken)
    {
        var agency = await FindAsync(id, cancellationToken);

        if (active) agency.Activate();
        else agency.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
        return MunicipalAgencyResponse.FromEntity(agency);
    }

    // Se o órgão ainda tem equipes, a FK (Restrict) recusa e a API responde 409.
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        db.MunicipalAgencies.Remove(await FindAsync(id, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<MunicipalAgency> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.MunicipalAgencies.FirstOrDefaultAsync(agency => agency.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Órgão {id} não encontrado.");
}
