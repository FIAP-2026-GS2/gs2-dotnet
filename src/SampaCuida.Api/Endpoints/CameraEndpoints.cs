using Microsoft.AspNetCore.Http.HttpResults;
using SampaCuida.Api.Validation;
using SampaCuida.Application.Cameras;
using SampaCuida.Application.Common.Paging;

namespace SampaCuida.Api.Endpoints;

public static class CameraEndpoints
{
    public static IEndpointRouteBuilder MapCameraEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cameras").WithTags("Câmeras");

        group.MapGet("/", List)
            .WithSummary("Lista as câmeras (paginado, com filtro por ponto e status)")
            .WithValidation<CameraQuery>();

        group.MapGet("/{id:int}", GetById)
            .WithSummary("Busca uma câmera pelo id")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithSummary("Cadastra uma câmera (o código é único: repetido responde 409)")
            .WithValidation<CreateCameraRequest>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:int}", Update)
            .WithSummary("Atualiza uma câmera, inclusive o status")
            .WithValidation<UpdateCameraRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:int}", Delete)
            .WithSummary("Remove uma câmera (recusado com 409 se ela já tiver agendas ou capturas)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<PagedResponse<CameraResponse>>> List(
        [AsParameters] CameraQuery query, ICameraService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(query, cancellationToken));

    private static async Task<Ok<CameraResponse>> GetById(
        int id, ICameraService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetByIdAsync(id, cancellationToken));

    private static async Task<Created<CameraResponse>> Create(
        CreateCameraRequest request, ICameraService service, CancellationToken cancellationToken)
    {
        var camera = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/cameras/{camera.Id}", camera);
    }

    private static async Task<Ok<CameraResponse>> Update(
        int id, UpdateCameraRequest request, ICameraService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken));

    private static async Task<NoContent> Delete(int id, ICameraService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
