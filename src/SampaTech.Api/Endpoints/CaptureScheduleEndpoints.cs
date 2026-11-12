using SampaTech.Application.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using SampaTech.Api.Validation;
using SampaTech.Application.CaptureSchedules;

namespace SampaTech.Api.Endpoints;

// Rotas aninhadas: a agenda só existe dentro de uma câmera.
public static class CaptureScheduleEndpoints
{
    public static IEndpointRouteBuilder MapCaptureScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cameras/{cameraId:int}/schedules").WithTags("Agendas de captura")
            .RequireAuthorization(AuthPolicies.Read);

        group.MapGet("/", List)
            .WithSummary("Lista as agendas de uma câmera")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:int}", GetById)
            .WithSummary("Busca uma agenda da câmera")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Cria uma agenda (12:00:00 ou 00:00:00, horário de São Paulo; sem dia da semana = todo dia)")
            .WithValidation<CaptureScheduleRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:int}", Update)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Atualiza uma agenda da câmera")
            .WithValidation<CaptureScheduleRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/activate", Activate)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Reativa uma agenda")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/deactivate", Deactivate)
            .RequireAuthorization(AuthPolicies.Manage)
            .WithSummary("Desativa uma agenda (para de gerar capturas, mas preserva o histórico)")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
            .RequireAuthorization(AuthPolicies.Delete)
            .WithSummary("Remove uma agenda (recusado com 409 se já tiver gerado capturas)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<CaptureScheduleResponse>>> List(
        int cameraId, ICaptureScheduleService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(cameraId, cancellationToken));

    private static async Task<Ok<CaptureScheduleResponse>> GetById(
        int cameraId, int id, ICaptureScheduleService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetByIdAsync(cameraId, id, cancellationToken));

    private static async Task<Created<CaptureScheduleResponse>> Create(
        int cameraId, CaptureScheduleRequest request, ICaptureScheduleService service,
        CancellationToken cancellationToken)
    {
        var schedule = await service.CreateAsync(cameraId, request, cancellationToken);
        return TypedResults.Created($"/api/cameras/{cameraId}/schedules/{schedule.Id}", schedule);
    }

    private static async Task<Ok<CaptureScheduleResponse>> Update(
        int cameraId, int id, CaptureScheduleRequest request, ICaptureScheduleService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(cameraId, id, request, cancellationToken));

    private static async Task<Ok<CaptureScheduleResponse>> Activate(
        int cameraId, int id, ICaptureScheduleService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(cameraId, id, true, cancellationToken));

    private static async Task<Ok<CaptureScheduleResponse>> Deactivate(
        int cameraId, int id, ICaptureScheduleService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(cameraId, id, false, cancellationToken));

    private static async Task<NoContent> Delete(
        int cameraId, int id, ICaptureScheduleService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(cameraId, id, cancellationToken);
        return TypedResults.NoContent();
    }
}
