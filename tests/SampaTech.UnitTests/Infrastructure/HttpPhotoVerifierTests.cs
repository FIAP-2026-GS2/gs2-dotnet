using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Photos;
using SampaTech.Domain.Enums;
using SampaTech.Infrastructure.Photos;

namespace SampaTech.UnitTests.Infrastructure;

public class HttpPhotoVerifierTests
{
    private static readonly PhotoUpload Photo = new("foto.jpg", [0xFF, 0xD8, 0xFF, 0xE0, 0x01]);
    private static readonly byte[] Blurred = [0xFF, 0xD8, 0xFF, 0xE0, 0x09];

    // "Rede" de mentira: responde o que o teste mandar, ou falha como se o serviço estivesse fora do ar.
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Received { get; private set; }
        public string ReceivedBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Received = request;
            ReceivedBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond(request);
        }
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpPhotoVerifier Create(FakeHandler handler, string? baseUrl = "http://ia.local") =>
        new(new HttpClient(handler),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiService:BaseUrl"] = baseUrl,
                ["AiService:ApiKey"] = "chave-de-teste"
            }).Build(),
            NullLogger<HttpPhotoVerifier>.Instance);

    private static Task<VerifiedPhoto> VerifyAsync(FakeHandler handler, string? baseUrl = "http://ia.local") =>
        Create(handler, baseUrl).VerifyAsync(
            Photo, PhotoFormat.Jpeg, IssueCategory.BURACO, CitizenFeedbackDecision.CONTEST, CancellationToken.None);

    [Fact]
    public async Task Foto_aceita_devolve_a_versao_borrada_e_manda_tipo_e_decisao()
    {
        var handler = new FakeHandler(_ => Json(
            $$"""{"valida": true, "motivo": "Foto conferida pela IA.", "foto_borrada_base64": "{{Convert.ToBase64String(Blurred)}}"}"""));

        var result = await VerifyAsync(handler);

        Assert.True(result.IsValid);
        Assert.Equal(Blurred, result.Content);
        Assert.Equal("http://ia.local/verificar-foto", handler.Received!.RequestUri!.ToString());
        Assert.Equal("chave-de-teste", Assert.Single(handler.Received.Headers.GetValues("X-API-Key")));
        Assert.Contains("name=categoria", handler.ReceivedBody);
        Assert.Contains("BURACO", handler.ReceivedBody);
        Assert.Contains("name=decisao", handler.ReceivedBody);
        Assert.Contains("CONTEST", handler.ReceivedBody);
    }

    [Fact]
    public async Task Foto_recusada_devolve_o_motivo_da_IA()
    {
        var handler = new FakeHandler(_ => Json(
            """{"valida": false, "motivo": "A foto não mostra uma via pública: um pássaro em um galho."}"""));

        var result = await VerifyAsync(handler);

        Assert.False(result.IsValid);
        Assert.Equal("A foto não mostra uma via pública: um pássaro em um galho.", result.Reason);
        Assert.Empty(result.Content);
    }

    [Fact]
    public async Task Recusa_sem_motivo_usa_uma_mensagem_padrao()
    {
        var result = await VerifyAsync(new FakeHandler(_ => Json("""{"valida": false}""")));

        Assert.Equal("A IA não reconheceu na foto o local da ocorrência.", result.Reason);
    }

    [Theory]
    [InlineData("""{"motivo": "sem veredito"}""")]
    [InlineData("""{"valida": true}""")]
    [InlineData("""{"valida": true, "foto_borrada_base64": "isto não é base64!"}""")]
    [InlineData("isto não é JSON")]
    public async Task Resposta_fora_do_contrato_vira_503(string body)
    {
        await Assert.ThrowsAsync<ServiceUnavailableException>(() => VerifyAsync(new FakeHandler(_ => Json(body))));
    }

    [Fact]
    public async Task Servico_fora_do_ar_vira_503()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("Conexão recusada"));

        await Assert.ThrowsAsync<ServiceUnavailableException>(() => VerifyAsync(handler));
    }

    [Fact]
    public async Task Resposta_de_erro_da_IA_vira_503()
    {
        // Ex.: a IA sem LLM ou sem borrão responde 503; para a .NET, é o mesmo "fora do ar".
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        await Assert.ThrowsAsync<ServiceUnavailableException>(() => VerifyAsync(handler));
    }

    [Fact]
    public async Task Sem_a_URL_da_IA_configurada_vira_503()
    {
        var handler = new FakeHandler(_ => Json("{}"));

        var exception = await Assert.ThrowsAsync<ServiceUnavailableException>(() => VerifyAsync(handler, baseUrl: null));

        Assert.Contains("não está configurado", exception.Message);
        Assert.Null(handler.Received);
    }
}
