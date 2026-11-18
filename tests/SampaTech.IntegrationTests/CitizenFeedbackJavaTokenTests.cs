using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SampaTech.Application.CitizenFeedbacks;
using SampaTech.Infrastructure.Persistence;
using SampaTech.IntegrationTests.Infrastructure;

namespace SampaTech.IntegrationTests;

/// <summary>O cidadão entra no app pela API Java, e a .NET aceita o mesmo token (só o papel CITIZEN).</summary>
public partial class CitizenFeedbackTests
{
    private const string Email = "ana.silva@email.com";

    [Theory]
    [InlineData(SecurityAlgorithms.HmacSha512)]
    [InlineData(SecurityAlgorithms.HmacSha256)]
    public async Task Cidadao_com_token_da_Java_envia_foto_e_ve_o_proprio_relato(string algorithm)
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = Factory.Client(JavaTokens.Create(Email, algorithm: algorithm));

        var response = await SendFeedbackAsync(citizen, issueId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var feedback = await response.ReadAsync<CitizenFeedbackResponse>();
        Assert.Equal(HttpStatusCode.OK, (await citizen.GetAsync($"/api/citizen-feedback/{feedback.Id}")).StatusCode);
    }

    [Fact]
    public async Task O_relato_guarda_um_codigo_e_nao_o_email_do_cidadao()
    {
        var issueId = await ResolvedIssueAsync();
        var sent = await (await SendFeedbackAsync(Factory.Client(JavaTokens.Create(Email)), issueId))
            .ReadAsync<CitizenFeedbackResponse>();

        using var scope = Factory.Services.CreateScope();
        var userId = await scope.ServiceProvider.GetRequiredService<AppDbContext>().CitizenFeedbacks
            .Where(item => item.Id == sent.Id).Select(item => item.UserId).SingleAsync();

        Assert.StartsWith("java:", userId);
        Assert.Equal(69, userId.Length);
        Assert.DoesNotContain("ana", userId);
    }

    [Fact]
    public async Task Um_cidadao_da_Java_nao_ve_o_relato_de_outro()
    {
        var issueId = await ResolvedIssueAsync();
        var sent = await (await SendFeedbackAsync(Factory.Client(JavaTokens.Create(Email)), issueId))
            .ReadAsync<CitizenFeedbackResponse>();

        var other = Factory.Client(JavaTokens.Create("bruno@email.com"));

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/citizen-feedback/{sent.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("AGENT")]
    public async Task Admin_e_agente_da_Java_nao_entram_na_NET(string role)
    {
        var issueId = await ResolvedIssueAsync();
        var javaUser = Factory.Client(JavaTokens.Create("gestor@prefeitura.sp.gov.br", role));

        Assert.Equal(HttpStatusCode.Unauthorized, (await SendFeedbackAsync(javaUser, issueId)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await javaUser.GetAsync("/api/citizen-feedback")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await javaUser.PatchAsJsonAsync("/api/citizen-feedback/1/review",
            new ReviewCitizenFeedbackRequest(true, null), Json.Options)).StatusCode);
    }

    [Fact]
    public async Task Cidadao_da_Java_nao_acessa_as_rotas_da_equipe()
    {
        var citizen = Factory.Client(JavaTokens.Create(Email));

        Assert.Equal(HttpStatusCode.Forbidden, (await citizen.GetAsync("/api/citizen-feedback")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await citizen.GetAsync("/api/cameras")).StatusCode);
    }

    [Fact]
    public async Task Token_da_Java_com_assinatura_errada_ou_vencido_responde_401()
    {
        var issueId = await ResolvedIssueAsync();
        var forged = Factory.Client(JavaTokens.Create(Email, secret: new string('x', 64)));
        var expired = Factory.Client(JavaTokens.Create(Email, expires: DateTime.UtcNow.AddMinutes(-10)));

        Assert.Equal(HttpStatusCode.Unauthorized, (await SendFeedbackAsync(forged, issueId)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendFeedbackAsync(expired, issueId)).StatusCode);
        Assert.Empty(Factory.Photos.StoredFiles);
    }
}
