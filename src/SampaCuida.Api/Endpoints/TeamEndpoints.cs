using SampaCuida.Application.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using SampaCuida.Api.Validation;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Application.Teams;

namespace SampaCuida.Api.Endpoints;

public static class TeamEndpoints
{
    public static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams").WithTags("Equipes")
            .RequireAuthorization(AuthPolicies.Read);

        group.MapGet("/", List)
            .WithSummary("Lista as equipes (paginado, com filtro por órgão e ativo)")
            .WithValidation<TeamQuery>();

        group.MapGet("/{id:int}", GetById)
            .WithSummary("Busca uma equipe pelo id")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Cadastra uma equipe (o órgão precisa existir)")
            .WithValidation<TeamRequest>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", Update)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Atualiza uma equipe")
            .WithValidation<TeamRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/activate", Activate)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Reativa uma equipe")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/deactivate", Deactivate)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Desativa uma equipe (preserva o histórico)")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
            .RequireAuthorization(AuthPolicies.Delete)
            .WithSummary("Remove uma equipe (recusado com 409 se já tiver atribuições)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<PagedResponse<TeamResponse>>> List(
        [AsParameters] TeamQuery query, ITeamService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(query, cancellationToken));

    private static async Task<Ok<TeamResponse>> GetById(
        int id, ITeamService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetByIdAsync(id, cancellationToken));

    private static async Task<Created<TeamResponse>> Create(
        TeamRequest request, ITeamService service, CancellationToken cancellationToken)
    {
        var team = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/teams/{team.Id}", team);
    }

    private static async Task<Ok<TeamResponse>> Update(
        int id, TeamRequest request, ITeamService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken));

    private static async Task<Ok<TeamResponse>> Activate(
        int id, ITeamService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(id, true, cancellationToken));

    private static async Task<Ok<TeamResponse>> Deactivate(
        int id, ITeamService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(id, false, cancellationToken));

    private static async Task<NoContent> Delete(int id, ITeamService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
