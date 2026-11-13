using SampaTech.Api.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;
using SampaTech.Api.Idempotency;
using SampaTech.Api.Validation;
using SampaTech.Application.Auth;
using SampaTech.Application.Captures;
using SampaTech.Application.Common.Paging;

namespace SampaTech.Api.Endpoints;

public static class CaptureEndpoints
{
    public static IEndpointRouteBuilder MapCaptureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/captures").WithTags("Capturas");

        group.MapGet("/", List)
            .WithSummary("Lista as capturas (mais recentes primeiro, com filtros por câmera, origem, status e período)")
            .WithValidation<CaptureQuery>()
            .RequireAuthorization(AuthPolicies.Read);

        group.MapGet("/{id:int}", GetById)
            .WithSummary("Busca uma captura com as suas imagens")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthPolicies.Read);

        // Usada pelo serviço de IA. Idempotente: repetir o envio com a mesma Idempotency-Key
        // devolve a resposta original sem gravar de novo.
        group.MapPost("/", Create)
            .WithSummary("Registra uma captura com a imagem anonimizada (exige o header Idempotency-Key)")
            .WithValidation<CreateCaptureRequest>()
            .WithIdempotency<CreateCaptureRequest>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireAuthorization(AuthPolicies.IaService)
            .RequireRateLimiting(RateLimitPolicies.IaService);

        return app;
    }

    private static async Task<Ok<PagedResponse<CaptureResponse>>> List(
        [AsParameters] CaptureQuery query, ICaptureService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(query, cancellationToken));

    private static async Task<Ok<CaptureResponse>> GetById(
        int id, ICaptureService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetByIdAsync(id, cancellationToken));

    private static async Task<Created<CaptureResponse>> Create(
        CreateCaptureRequest request, ICaptureService service, CancellationToken cancellationToken)
    {
        var capture = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/captures/{capture.Id}", capture);
    }
}
