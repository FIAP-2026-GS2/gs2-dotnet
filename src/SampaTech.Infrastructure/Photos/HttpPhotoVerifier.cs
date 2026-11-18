using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Photos;
using SampaTech.Domain.Enums;

namespace SampaTech.Infrastructure.Photos;

/// <summary>
/// Pede ao serviço de IA (Python) para borrar a foto e conferir se ela serve como prova:
/// POST {AiService:BaseUrl}/verificar-foto, multipart com "file", "categoria" e "decisao", e a chave
/// no header X-API-Key (AiService:ApiKey). A resposta é um JSON com o veredito, o motivo e a foto borrada.
/// Qualquer falha (sem configuração, fora do ar, demora, resposta inválida) vira 503 e nada é guardado.
/// </summary>
public class HttpPhotoVerifier(HttpClient http, IConfiguration configuration, ILogger<HttpPhotoVerifier> logger)
    : IPhotoVerifier
{
    private const string DefaultRejection = "A IA não reconheceu na foto o local da ocorrência.";

    public async Task<VerifiedPhoto> VerifyAsync(
        PhotoUpload photo,
        string contentType,
        IssueCategory category,
        CitizenFeedbackDecision decision,
        CancellationToken cancellationToken)
    {
        if (configuration["AiService:BaseUrl"] is not { Length: > 0 } baseUrl)
            throw new ServiceUnavailableException(
                "O serviço de IA não está configurado para conferir a foto. Nada foi guardado.");

        var file = new ByteArrayContent(photo.Content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var form = new MultipartFormDataContent
        {
            { file, "file", photo.FileName },
            { new StringContent(category.ToString()), "categoria" },
            { new StringContent(decision.ToString()), "decisao" }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post, new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "verificar-foto"))
        {
            Content = form
        };

        // As rotas do serviço de IA são protegidas por chave (segredo: user-secrets ou variável de ambiente).
        if (configuration["AiService:ApiKey"] is { Length: > 0 } apiKey)
            request.Headers.Add("X-API-Key", apiKey);

        AiVerdict? verdict;
        try
        {
            using var response = await http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("O serviço de IA respondeu {Status} ao conferir a foto.", (int)response.StatusCode);
                throw Unavailable();
            }

            verdict = await response.Content.ReadFromJsonAsync<AiVerdict>(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or NotSupportedException
                                              || (exception is TaskCanceledException
                                                  && !cancellationToken.IsCancellationRequested))
        {
            // O motivo vai para o log; quem chamou recebe só a mensagem amigável.
            logger.LogWarning(exception, "O serviço de IA não respondeu ao conferir a foto.");
            throw Unavailable();
        }

        if (verdict?.Valid is null)
        {
            logger.LogWarning("O serviço de IA respondeu sem o veredito da foto.");
            throw Unavailable();
        }

        if (verdict.Valid == false)
            return new VerifiedPhoto(false, string.IsNullOrWhiteSpace(verdict.Reason) ? DefaultRejection : verdict.Reason, []);

        // Aceita: precisa vir a foto borrada, que é a única versão guardada.
        try
        {
            var blurred = Convert.FromBase64String(verdict.BlurredPhotoBase64 ?? "");
            if (blurred.Length == 0)
                throw new FormatException("foto vazia");

            return new VerifiedPhoto(true, verdict.Reason ?? "", blurred);
        }
        catch (FormatException exception)
        {
            logger.LogWarning(exception, "O serviço de IA aceitou a foto, mas não devolveu a versão borrada.");
            throw Unavailable();
        }
    }

    private static ServiceUnavailableException Unavailable() =>
        new("O serviço de IA não conseguiu conferir a foto agora. Nada foi guardado; tente de novo em instantes.");

    // Só os campos que a .NET usa; o resto da resposta (descrição, contagens, LLM) fica nos logs da IA.
    private sealed record AiVerdict(
        [property: JsonPropertyName("valida")] bool? Valid,
        [property: JsonPropertyName("motivo")] string? Reason,
        [property: JsonPropertyName("foto_borrada_base64")] string? BlurredPhotoBase64);
}
