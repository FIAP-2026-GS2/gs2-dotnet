using Microsoft.AspNetCore.Http.HttpResults;
using SampaCuida.Api.Validation;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Application.MunicipalAgencies;

namespace SampaCuida.Api.Endpoints;

public static class MunicipalAgencyEndpoints
{
    public static IEndpointRouteBuilder MapMunicipalAgencyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/municipal-agencies").WithTags("Órgãos da prefeitura");

        group.MapGet("/", List)
            .WithSummary("Lista os órgãos (paginado, com filtro por ativo)")
            .WithValidation<MunicipalAgencyQuery>();

        group.MapGet("/{id:int}", GetById)
            .WithSummary("Busca um órgão pelo id")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithSummary("Cadastra um órgão")
            .WithValidation<MunicipalAgencyRequest>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", Update)
            .WithSummary("Atualiza um órgão")
            .WithValidation<MunicipalAgencyRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/activate", Activate)
            .WithSummary("Reativa um órgão")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/deactivate", Deactivate)
            .WithSummary("Desativa um órgão (preserva o histórico)")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
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
