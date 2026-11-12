using SampaTech.Api.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;
using SampaTech.Application.Auth;
using SampaTech.Application.Schedules;

namespace SampaTech.Api.Endpoints;

public static class DueScheduleEndpoints
{
    public static IEndpointRouteBuilder MapDueScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        // Usada pelo serviço de IA: "quais frames devo extrair agora?".
        app.MapGet("/api/schedules/due", ListDue)
            .WithTags("Agendas de captura")
            .WithSummary("Lista os frames a extrair agora: agendas de 12:00/00:00 (horário de São Paulo) " +
                         "e alertas de chuva ativos. Use 'at' para simular outro horário.")
            .RequireAuthorization(AuthPolicies.IaService)
            .RequireRateLimiting(RateLimitPolicies.IaService);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<DueCaptureResponse>>> ListDue(
        [AsParameters] DueScheduleQuery query, IDueScheduleService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListDueAsync(query, cancellationToken));
}
