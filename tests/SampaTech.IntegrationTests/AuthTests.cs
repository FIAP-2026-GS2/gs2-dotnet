using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SampaTech.Application.Auth;
using SampaTech.IntegrationTests.Infrastructure;

namespace SampaTech.IntegrationTests;

public class AuthTests(ApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Login_valido_devolve_token_e_papel()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestUsers.Manager, TestUsers.Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.ReadAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.Equal("Bearer", login.TokenType);
        Assert.Equal(Roles.Manager, login.Role);
    }

    [Fact]
    public async Task Senha_errada_responde_401_sem_dizer_qual_campo_errou()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestUsers.Manager, "senha-errada"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>();
        Assert.Equal("Usuário ou senha inválidos.", problem.Detail);
    }

    [Fact]
    public async Task Rota_protegida_sem_token_responde_401_com_WWW_Authenticate()
    {
        var response = await Anonymous().GetAsync("/api/cameras");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }

    [Fact]
    public async Task Token_adulterado_responde_401()
    {
        var client = await AsAsync(TestUsers.Admin);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token[..^4] + "AAAA");

        var response = await client.GetAsync("/api/cameras");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_le_mas_nao_cadastra()
    {
        var viewer = await AsAsync(TestUsers.Viewer);

        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/cameras")).StatusCode);
        var create = await viewer.PostAsJsonAsync("/api/cameras", new { code = "CAM-X", monitoringPointId = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Manager_nao_apaga_e_recebe_a_orientacao_de_desativar()
    {
        var manager = await AsAsync(TestUsers.Manager);

        var response = await manager.DeleteAsync("/api/cameras/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>();
        Assert.Contains("Só o ADMIN pode apagar", problem.Detail);
    }

    [Fact]
    public async Task Servico_de_IA_nao_acessa_as_rotas_da_prefeitura()
    {
        var ia = await AsAsync(TestUsers.IaService);

        Assert.Equal(HttpStatusCode.Forbidden, (await ia.GetAsync("/api/cameras")).StatusCode);
    }

    [Fact]
    public async Task So_o_servico_de_IA_registra_capturas()
    {
        var manager = await AsAsync(TestUsers.Manager);

        var response = await manager.PostIdempotentAsync("/api/captures", Seed.Capture(1, 1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Rota_publica_do_cidadao_nao_exige_login()
    {
        var response = await Anonymous().GetAsync("/api/public/issue-reports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
