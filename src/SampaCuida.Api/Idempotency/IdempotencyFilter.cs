using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SampaCuida.Application.Common.Idempotency;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace SampaCuida.Api.Idempotency;

/// <summary>
/// Idempotência por header. A mesma Idempotency-Key com o mesmo conteúdo devolve a
/// resposta original sem gravar de novo; com outro conteúdo, responde 422.
/// </summary>
public class IdempotencyFilter<TRequest> : IEndpointFilter
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayedHeaderName = "Idempotency-Replayed";
    private const int KeyMaxLength = 100;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var key = http.Request.Headers[HeaderName].ToString().Trim();

        if (key.Length is 0 or > KeyMaxLength)
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [HeaderName] = [$"O header {HeaderName} é obrigatório neste endpoint (até {KeyMaxLength} caracteres)."]
                },
                title: "Dados inválidos");

        var store = http.RequestServices.GetRequiredService<IIdempotencyStore>();
        var jsonOptions = http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;

        // A impressão digital considera método, rota (inclui ids da URL) e corpo já convertido.
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        var requestHash = Hash($"{http.Request.Method} {http.Request.Path}|{JsonSerializer.Serialize(request, jsonOptions)}");

        var check = await store.TryBeginAsync(key, requestHash, http.RequestAborted);

        switch (check.Kind)
        {
            case IdempotencyCheckKind.Replay:
                http.Response.Headers[ReplayedHeaderName] = "true";
                return Results.Content(check.ResponseBody, "application/json", Encoding.UTF8, check.StatusCode);

            case IdempotencyCheckKind.Mismatch:
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Chave de idempotência reutilizada",
                    detail: $"Essa {HeaderName} já foi usada com outro conteúdo. Gere uma chave nova para uma operação nova.");

            case IdempotencyCheckKind.InProgress:
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Requisição em processamento",
                    detail: $"Uma requisição com essa {HeaderName} ainda está sendo processada. Tente de novo em instantes.");
        }

        object? result;
        try
        {
            result = await next(context);
        }
        catch
        {
            // Falhou: libera a chave para a nova tentativa funcionar. O erro segue para o tratador global.
            await store.ReleaseAsync(key, CancellationToken.None);
            throw;
        }

        // Só respostas de sucesso ficam guardadas para as repetições.
        if (result is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 } success and IValueHttpResult withValue)
            await store.CompleteAsync(
                key, success.StatusCode!.Value, JsonSerializer.Serialize(withValue.Value, jsonOptions), CancellationToken.None);
        else
            await store.ReleaseAsync(key, CancellationToken.None);

        return result;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

/// <summary>Marca a rota como idempotente (usado pelo Swagger para mostrar o header).</summary>
public sealed class RequiresIdempotencyKeyMetadata;

public static class IdempotencyFilterExtensions
{
    // Uso: .WithIdempotency<MeuRequest>(), depois do .WithValidation<MeuRequest>():
    // pedido inválido nem chega a reservar a chave.
    public static RouteHandlerBuilder WithIdempotency<TRequest>(this RouteHandlerBuilder builder) =>
        builder
            .AddEndpointFilter<IdempotencyFilter<TRequest>>()
            .WithMetadata(new RequiresIdempotencyKeyMetadata())
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
