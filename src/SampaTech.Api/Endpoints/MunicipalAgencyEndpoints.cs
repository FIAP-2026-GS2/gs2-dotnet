using SampaTech.Application.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using SampaTech.Api.Validation;
using SampaTech.Application.Common.Paging;
using SampaTech.Application.MunicipalAgencies;

namespace SampaTech.Api.Endpoints;

public static class MunicipalAgencyEndpoints
{
    public static IEndpointRouteBuilder MapMunicipalAgencyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/municipal-agencies").WithTags("Órgãos da prefeitura")
            .RequireAuthorization(AuthPolicies.Read);

        group.MapGet("/", List)
            .WithSummary("Lista os órgãos (paginado, com filtro por ativo)")
            .WithValidation<MunicipalAgencyQuery>();

        group.MapGet("/{id:int}", GetById)
            .WithSummary("Busca um órgão pelo id")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Cadastra um órgão")
            .WithValidation<MunicipalAgencyRequest>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", Update)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Atualiza um órgão")
            .WithValidation<MunicipalAgencyRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/activate", Activate)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Reativa um órgão")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/deactivate", Deactivate)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Desativa um órgão (preserva o histórico)")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
            .RequireAuthorization(AuthPolicies.Delete)
            .WithSummary("Remove um órgão (recusado com 409 se ainda tiver equipes)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<PagedResponse<MunicipalAgencyResponse>>> List(
        [AsParameters] MunicipalAgencyQuery query, IMunicipalAgencyService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(query, cancellationToken));

    private static async Task<Ok<MunicipalAgencyResponse>> GetById(
        int id, IMunicipalAgencyService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetByIdAsync(id, cancellationToken));

    private static async Task<Created<MunicipalAgencyResponse>> Create(
        MunicipalAgencyRequest request, IMunicipalAgencyService service, CancellationToken cancellationToken)
    {
        var agency = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/municipal-agencies/{agency.Id}", agency);
    }

    private static async Task<Ok<MunicipalAgencyResponse>> Update(
        int id, MunicipalAgencyRequest request, IMunicipalAgencyService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken));

    private static async Task<Ok<MunicipalAgencyResponse>> Activate(
        int id, IMunicipalAgencyService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(id, true, cancellationToken));

    private static async Task<Ok<MunicipalAgencyResponse>> Deactivate(
        int id, IMunicipalAgencyService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(id, false, cancellationToken));

    private static async Task<NoContent> Delete(
        int id, IMunicipalAgencyService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
