using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SampaTech.Application.Auth;
using SampaTech.IntegrationTests.Infrastructure;

namespace SampaTech.IntegrationTests;

[CollectionDefinition(Name)]
public class RateLimitCollection : ICollectionFixture<RateLimitedApiFactory>
{
    public const string Name = "rate-limit";
}

/// <summary>API com limite baixo (3) de propósito, em banco separado dos demais testes.</summary>
[Collection(RateLimitCollection.Name)]
public class RateLimitTests(RateLimitedApiFactory factory)
{
    [Fact]
    public async Task Login_alem_do_limite_responde_429_com_Retry_After()
    {
        var client = factory.CreateClient();
        var attempt = new LoginRequest("ninguem", "senha-errada");

        for (var i = 0; i < RateLimitedApiFactory.Limit; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", attempt)).StatusCode);

        var blocked = await client.PostAsJsonAsync("/api/auth/login", attempt);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        var problem = await blocked.ReadAsync<ProblemDetails>();
        Assert.Equal("Muitas requisições", problem.Title);
        Assert.Equal(429, problem.Status);
    }

    [Fact]
    public async Task Rota_publica_alem_do_limite_responde_429()
    {
        var client = factory.CreateClient();

        for (var i = 0; i < RateLimitedApiFactory.Limit; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/public/issue-reports")).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/public/issue-reports")).StatusCode);
    }

    [Fact]
    public async Task Health_check_nao_tem_limite()
    {
        var client = factory.CreateClient();

        for (var i = 0; i < RateLimitedApiFactory.Limit * 10; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }
}

public class HealthTests(ApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Live_responde_sem_login_e_sem_testar_dependencias()
    {
        var response = await Anonymous().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        Assert.Equal(0, body.GetProperty("checks").GetArrayLength());
    }

    [Fact]
    public async Task Health_verifica_o_banco_sem_login()
    {
        var response = await Anonymous().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        var check = Assert.Single(body.GetProperty("checks").EnumerateArray());
        Assert.Equal("database", check.GetProperty("name").GetString());
        Assert.Equal("Healthy", check.GetProperty("status").GetString());
    }
}
