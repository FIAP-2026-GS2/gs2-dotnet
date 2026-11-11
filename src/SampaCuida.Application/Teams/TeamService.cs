using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SampaCuida.Application.Abstractions;
using SampaCuida.Application.Common.Exceptions;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Application.Teams;

public record TeamRequest(string Name, int MunicipalAgencyId, string? Description);

public record TeamResponse(int Id, string Name, string? Description, bool IsActive, int MunicipalAgencyId)
{
    public static TeamResponse FromEntity(Team team) =>
        new(team.Id, team.Name, team.Description, team.IsActive, team.MunicipalAgencyId);
}

public record TeamQuery(int Page = 1, int PageSize = 10, int? MunicipalAgencyId = null, bool? IsActive = null);

public class TeamRequestValidator : AbstractValidator<TeamRequest>
{
    public TeamRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Team.NameMaxLength).WithName("nome");
        RuleFor(x => x.MunicipalAgencyId).GreaterThan(0).WithName("órgão");
        RuleFor(x => x.Description).MaximumLength(Team.DescriptionMaxLength).WithName("descrição");
    }
}

public class TeamQueryValidator : AbstractValidator<TeamQuery>
{
    public TeamQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingExtensions.MaxPageSize).WithName("tamanho da página");
    }
}

public interface ITeamService
{
    Task<PagedResponse<TeamResponse>> ListAsync(TeamQuery query, CancellationToken cancellationToken);
    Task<TeamResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<TeamResponse> CreateAsync(TeamRequest request, CancellationToken cancellationToken);
    Task<TeamResponse> UpdateAsync(int id, TeamRequest request, CancellationToken cancellationToken);
    Task<TeamResponse> SetActiveAsync(int id, bool active, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

public class TeamService(IAppDbContext db) : ITeamService
{
    public async Task<PagedResponse<TeamResponse>> ListAsync(TeamQuery query, CancellationToken cancellationToken)
    {
        var teams = db.Teams.AsNoTracking();

        if (query.MunicipalAgencyId is not null)
            teams = teams.Where(team => team.MunicipalAgencyId == query.MunicipalAgencyId);

        if (query.IsActive is not null)
            teams = teams.Where(team => team.IsActive == query.IsActive);

        return await teams
            .OrderBy(team => team.Id)
            .ToPagedAsync(query.Page, query.PageSize, TeamResponse.FromEntity, cancellationToken);
    }

    public async Task<TeamResponse> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        TeamResponse.FromEntity(await FindAsync(id, cancellationToken));

    public async Task<TeamResponse> CreateAsync(TeamRequest request, CancellationToken cancellationToken)
    {
        await EnsureAgencyExistsAsync(request.MunicipalAgencyId, cancellationToken);

        var team = new Team(request.Name, request.MunicipalAgencyId, request.Description);

        db.Teams.Add(team);
        await db.SaveChangesAsync(cancellationToken);

        return TeamResponse.FromEntity(team);
    }

    public async Task<TeamResponse> UpdateAsync(int id, TeamRequest request, CancellationToken cancellationToken)
    {
        var team = await FindAsync(id, cancellationToken);
        await EnsureAgencyExistsAsync(request.MunicipalAgencyId, cancellationToken);

        team.Update(request.Name, request.MunicipalAgencyId, request.Description);

        await db.SaveChangesAsync(cancellationToken);
        return TeamResponse.FromEntity(team);
    }

    public async Task<TeamResponse> SetActiveAsync(int id, bool active, CancellationToken cancellationToken)
    {
        var team = await FindAsync(id, cancellationToken);

        if (active) team.Activate();
        else team.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
        return TeamResponse.FromEntity(team);
    }

    // Se a equipe já tem atribuições, a FK (Restrict) recusa e a API responde 409.
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        db.Teams.Remove(await FindAsync(id, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Team> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Teams.FirstOrDefaultAsync(team => team.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Equipe {id} não encontrada.");

    private async Task EnsureAgencyExistsAsync(int municipalAgencyId, CancellationToken cancellationToken)
    {
        if (!await db.MunicipalAgencies.AnyAsync(agency => agency.Id == municipalAgencyId, cancellationToken))
            throw new DomainException($"O órgão {municipalAgencyId} não existe.");
    }
}
