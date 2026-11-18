using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Photos;
using SampaTech.Infrastructure.Photos;

namespace SampaTech.UnitTests.Infrastructure;

public class HttpPhotoAnonymizerTests
{
    private static readonly PhotoUpload Photo = new("foto.jpg", [0xFF, 0xD8, 0xFF, 0xE0, 0x01]);

    // "Rede" de mentira: responde o que o teste mandar, ou falha como se o serviço estivesse fora do ar.
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Received { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Received = request;
            return Task.FromResult(respond(request));
        }
    }

    private static HttpPhotoAnonymizer Create(FakeHandler handler, string? baseUrl = "http://ia.local") =>
        new(new HttpClient(handler),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiService:BaseUrl"] = baseUrl,
                ["AiService:ApiKey"] = "chave-de-teste"
            }).Build(),
            NullLogger<HttpPhotoAnonymizer>.Instance);

    [Fact]
    public async Task Envia_a_foto_para_anonymize_e_devolve_a_imagem_borrada()
    {
        byte[] blurred = [0xFF, 0xD8, 0xFF, 0xE0, 0x09];
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(blurred) });

        var result = await Create(handler).AnonymizeAsync(Photo, PhotoFormat.Jpeg, CancellationToken.None);

        Assert.Equal(blurred, result);
        Assert.Equal("http://ia.local/anonymize", handler.Received!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.Received.Method);
        Assert.Equal("chave-de-teste", Assert.Single(handler.Received.Headers.GetValues("X-API-Key")));
    }

    [Fact]
    public async Task Servico_fora_do_ar_vira_503_sem_guardar_nada()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("Conexão recusada"));

        await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => Create(handler).AnonymizeAsync(Photo, PhotoFormat.Jpeg, CancellationToken.None));
    }

    [Fact]
    public async Task Resposta_de_erro_da_IA_vira_503()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => Create(handler).AnonymizeAsync(Photo, PhotoFormat.Jpeg, CancellationToken.None));
    }

    [Fact]
    public async Task Sem_a_URL_da_IA_configurada_vira_503()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var exception = await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => Create(handler, baseUrl: null).AnonymizeAsync(Photo, PhotoFormat.Jpeg, CancellationToken.None));

        Assert.Contains("não está configurado", exception.Message);
        Assert.Null(handler.Received);
    }
}
