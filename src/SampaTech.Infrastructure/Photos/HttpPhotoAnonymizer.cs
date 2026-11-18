using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Photos;

namespace SampaTech.Infrastructure.Photos;

/// <summary>
/// Pede ao serviço de IA (Python) para borrar rostos e placas:
/// POST {AiService:BaseUrl}/anonymize, multipart com o campo "file" e a chave no header X-API-Key
/// (AiService:ApiKey); a resposta é a imagem borrada.
/// Qualquer falha (sem configuração, fora do ar, demora, resposta inválida) vira 503 e nada é guardado.
/// </summary>
public class HttpPhotoAnonymizer(HttpClient http, IConfiguration configuration, ILogger<HttpPhotoAnonymizer> logger)
    : IPhotoAnonymizer
{
    public const string ClientName = "ai-anonymizer";

    public async Task<byte[]> AnonymizeAsync(PhotoUpload photo, string contentType, CancellationToken cancellationToken)
    {
        if (configuration["AiService:BaseUrl"] is not { Length: > 0 } baseUrl)
            throw new ServiceUnavailableException(
                "O serviço de IA não está configurado para borrar a foto. Nada foi guardado.");

        var file = new ByteArrayContent(photo.Content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var form = new MultipartFormDataContent { { file, "file", photo.FileName } };

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "anonymize"))
        {
            Content = form
        };

        // As rotas do serviço de IA são protegidas por chave (segredo: user-secrets ou variável de ambiente).
        if (configuration["AiService:ApiKey"] is { Length: > 0 } apiKey)
            request.Headers.Add("X-API-Key", apiKey);

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("O serviço de IA respondeu {Status} ao anonimizar a foto.", (int)response.StatusCode);
                throw Unavailable();
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
                                              && !cancellationToken.IsCancellationRequested)
        {
            // O motivo vai para o log; quem chamou recebe só a mensagem amigável.
            logger.LogWarning(exception, "O serviço de IA não respondeu ao anonimizar a foto.");
            throw Unavailable();
        }
    }

    private static ServiceUnavailableException Unavailable() =>
        new("O serviço de IA não conseguiu borrar a foto agora. Nada foi guardado; tente de novo em instantes.");
}
