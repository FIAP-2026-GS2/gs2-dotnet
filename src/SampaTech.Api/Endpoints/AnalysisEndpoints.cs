using SampaTech.Api.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;
using SampaTech.Api.Idempotency;
using SampaTech.Api.Validation;
using SampaTech.Application.Analyses;
using SampaTech.Application.Auth;

namespace SampaTech.Api.Endpoints;

public static class AnalysisEndpoints
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analyses").WithTags("Análises da IA");

        group.MapGet("/{id:int}", GetById)
            .WithSummary("Busca uma análise com as suas detecções")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthPolicies.Read);

        // Usada pelo serviço de IA. Abre ocorrências ou soma nas abertas (deduplicação).
        group.MapPost("/", Create)
            .WithSummary("Registra a análise de uma imagem e processa as detecções (exige o header Idempotency-Key)")
            .WithValidation<CreateAnalysisRequest>()
            .WithIdempotency<CreateAnalysisRequest>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireAuthorization(AuthPolicies.IaService)
            .RequireRateLimiting(RateLimitPolicies.IaService);

        return app;
    }

    private static async Task<Ok<AnalysisResponse>> GetById(
        int id, IAnalysisService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetByIdAsync(id, cancellationToken));

    private static async Task<Created<AnalysisResultResponse>> Create(
        CreateAnalysisRequest request, IAnalysisService service, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/analyses/{result.AnalysisId}", result);
    }
}
