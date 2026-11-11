using Microsoft.AspNetCore.Http.HttpResults;
using SampaCuida.Api.Validation;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Application.MonitoringPoints;

namespace SampaCuida.Api.Endpoints;

public static class MonitoringPointEndpoints
{
    public static IEndpointRouteBuilder MapMonitoringPointEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/monitoring-points").WithTags("Pontos monitorados");

        group.MapGet("/", List)
            .WithSummary("Lista os pontos monitorados (paginado, com filtro por bairro e cidade)")
            .WithValidation<MonitoringPointQuery>();

        group.MapGet("/{id:int}", GetById)
            .WithSummary("Busca um ponto monitorado pelo id")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithSummary("Cadastra um ponto monitorado")
            .WithValidation<MonitoringPointRequest>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", Update)
            .WithSummary("Atualiza um ponto monitorado")
            .WithValidation<MonitoringPointRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
            .WithSummary("Remove um ponto monitorado (recusado com 409 se ainda tiver câmeras)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<PagedResponse<MonitoringPointResponse>>> List(
        [AsParameters] MonitoringPointQuery query, IMonitoringPointService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(query, cancellationToken));

    private static async Task<Ok<MonitoringPointResponse>> GetById(
        int id, IMonitoringPointService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetByIdAsync(id, cancellationToken));

    private static async Task<Created<MonitoringPointResponse>> Create(
        MonitoringPointRequest request, IMonitoringPointService service, CancellationToken cancellationToken)
    {
        var point = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/monitoring-points/{point.Id}", point);
    }

    private static async Task<Ok<MonitoringPointResponse>> Update(
        int id, MonitoringPointRequest request, IMonitoringPointService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken));

    private static async Task<NoContent> Delete(
        int id, IMonitoringPointService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
