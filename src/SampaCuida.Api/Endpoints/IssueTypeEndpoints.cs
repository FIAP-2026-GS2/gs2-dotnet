using Microsoft.AspNetCore.Http.HttpResults;
using SampaCuida.Api.Validation;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Application.IssueTypes;

namespace SampaCuida.Api.Endpoints;

public static class IssueTypeEndpoints
{
    public static IEndpointRouteBuilder MapIssueTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/issue-types").WithTags("Tipos de problema");

        group.MapGet("/", List)
            .WithSummary("Lista os tipos de problema (paginado, com filtro por categoria e ativo)")
            .WithValidation<IssueTypeQuery>();

        group.MapGet("/{id:int}", GetById)
            .WithSummary("Busca um tipo de problema pelo id")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithSummary("Cadastra um tipo de problema (o nome é único: repetido responde 409)")
            .WithValidation<IssueTypeRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:int}", Update)
            .WithSummary("Atualiza um tipo de problema")
            .WithValidation<IssueTypeRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{id:int}/activate", Activate)
            .WithSummary("Reativa um tipo de problema")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/deactivate", Deactivate)
            .WithSummary("Desativa um tipo de problema (preserva o histórico)")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
            .WithSummary("Remove um tipo de problema (recusado com 409 se já estiver em uso)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<PagedResponse<IssueTypeResponse>>> List(
        [AsParameters] IssueTypeQuery query, IIssueTypeService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(query, cancellationToken));

    private static async Task<Ok<IssueTypeResponse>> GetById(
        int id, IIssueTypeService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetByIdAsync(id, cancellationToken));

    private static async Task<Created<IssueTypeResponse>> Create(
        IssueTypeRequest request, IIssueTypeService service, CancellationToken cancellationToken)
    {
        var issueType = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/issue-types/{issueType.Id}", issueType);
    }

    private static async Task<Ok<IssueTypeResponse>> Update(
        int id, IssueTypeRequest request, IIssueTypeService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken));

    private static async Task<Ok<IssueTypeResponse>> Activate(
        int id, IIssueTypeService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(id, true, cancellationToken));

    private static async Task<Ok<IssueTypeResponse>> Deactivate(
        int id, IIssueTypeService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(id, false, cancellationToken));

    private static async Task<NoContent> Delete(int id, IIssueTypeService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
