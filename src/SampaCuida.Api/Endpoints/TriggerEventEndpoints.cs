using Microsoft.AspNetCore.Http.HttpResults;
using SampaCuida.Api.Validation;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Application.TriggerEvents;

namespace SampaCuida.Api.Endpoints;

public static class TriggerEventEndpoints
{
    public static IEndpointRouteBuilder MapTriggerEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/trigger-events").WithTags("Alertas de chuva");

        group.MapGet("/", List)
            .WithSummary("Lista os alertas (mais recentes primeiro, com filtro por status)")
            .WithValidation<TriggerEventQuery>();

        group.MapGet("/{id:int}", GetById)
            .WithSummary("Busca um alerta pelo id")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithSummary("Registra um alerta (sem data de início, vale o momento atual)")
            .WithValidation<CreateTriggerEventRequest>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", Update)
            .WithSummary("Atualiza fonte, descrição e intensidade de um alerta ativo")
            .WithValidation<UpdateTriggerEventRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/end", End)
            .WithSummary("Encerra um alerta (sem data de fim, vale o momento atual; encerrar duas vezes responde 400)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:int}", Delete)
            .WithSummary("Remove um alerta (recusado com 409 se já tiver gerado capturas)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<PagedResponse<TriggerEventResponse>>> List(
        [AsParameters] TriggerEventQuery query, ITriggerEventService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(query, cancellationToken));

    private static async Task<Ok<TriggerEventResponse>> GetById(
        int id, ITriggerEventService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetByIdAsync(id, cancellationToken));

    private static async Task<Created<TriggerEventResponse>> Create(
        CreateTriggerEventRequest request, ITriggerEventService service, CancellationToken cancellationToken)
    {
        var triggerEvent = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/trigger-events/{triggerEvent.Id}", triggerEvent);
    }

    private static async Task<Ok<TriggerEventResponse>> Update(
        int id, UpdateTriggerEventRequest request, ITriggerEventService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken));

    // O corpo é opcional: sem corpo, encerra com o momento atual.
    private static async Task<Ok<TriggerEventResponse>> End(
        int id, EndTriggerEventRequest? request, ITriggerEventService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.EndAsync(id, request ?? new EndTriggerEventRequest(null), cancellationToken));

    private static async Task<NoContent> Delete(int id, ITriggerEventService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
