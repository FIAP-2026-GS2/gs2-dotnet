using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SampaTech.Infrastructure.HealthChecks;

namespace SampaTech.Api.HealthChecks;

public static class HealthEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Pulso: "o processo está de pé?". Não toca em nenhuma dependência, então um banco fora do ar
        // não faz o Azure reiniciar a API à toa (reiniciar não conserta o banco).
        app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false,
                ResponseWriter = WriteJsonAsync
            })
            .AllowAnonymous()
            .DisableRateLimiting();

        // Exame completo: banco e serviços externos configurados.
        // Healthy e Degraded respondem 200; Unhealthy responde 503.
        app.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(HealthCheckTags.Ready),
                ResponseWriter = WriteJsonAsync
            })
            .AllowAnonymous()
            .DisableRateLimiting();

        return app;
    }

    // Só nome, status e duração: o motivo da falha (mensagem, exceção) vai para o log, não para
    // a resposta, que é pública e não pode revelar servidor, usuário ou detalhes internos.
    private static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var body = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1)
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
