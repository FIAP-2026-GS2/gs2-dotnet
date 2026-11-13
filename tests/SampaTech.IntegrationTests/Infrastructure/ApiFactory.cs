using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;
using SampaTech.Application.Auth;
using SampaTech.Infrastructure.Auth;
using SampaTech.Infrastructure.Persistence;

namespace SampaTech.IntegrationTests.Infrastructure;

/// <summary>Usuários de teste, um por papel. A senha só existe aqui; a API recebe o hash.</summary>
public static class TestUsers
{
    public const string Password = "Senha-de-teste-123";

    public const string Admin = "teste-admin";
    public const string Manager = "teste-manager";
    public const string Viewer = "teste-viewer";
    public const string IaService = "teste-ia";

    public static readonly (string Username, string Role)[] All =
    [
        (Admin, Roles.Admin), (Manager, Roles.Manager), (Viewer, Roles.Viewer), (IaService, Roles.IaService)
    ];
}

/// <summary>
/// Sobe a API inteira em memória (WebApplicationFactory) apontando para um banco próprio no
/// SQL Server do container. Ambiente "Testing": não lê o user-secrets de ninguém.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string PasswordHash = PasswordHashing.Hash(TestUsers.Password);

    private readonly string _jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private readonly Dictionary<string, string> _tokens = [];
    private string _connectionString = "";
    private Respawner? _respawner;

    /// <summary>Limites altos: os testes comuns não podem esbarrar no rate limit.</summary>
    protected virtual IReadOnlyDictionary<string, string> RateLimits => new Dictionary<string, string>
    {
        ["RateLimiting:Login:PermitLimit"] = "1000",
        ["RateLimiting:IaService:TokenLimit"] = "1000",
        ["RateLimiting:Public:PermitLimit"] = "1000",
        ["RateLimiting:Default:PermitLimit"] = "1000"
    };

    public async Task InitializeAsync()
    {
        // Banco com nome único: fábricas diferentes não se enxergam.
        var server = await SharedSqlServer.GetConnectionStringAsync();
        _connectionString = new SqlConnectionStringBuilder(server)
        {
            InitialCatalog = $"SampaTechTests_{Guid.NewGuid():N}"
        }.ConnectionString;

        // As mesmas migrations da produção criam o banco.
        using (var scope = Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    /// <summary>Apaga os dados de todas as tabelas (a estrutura fica): cada teste começa do zero.</summary>
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }

    /// <summary>Cliente já logado com o usuário do papel (login de verdade; o token é reaproveitado).</summary>
    public async Task<HttpClient> ClientAsAsync(string username)
    {
        if (!_tokens.TryGetValue(username, out var token))
        {
            var response = await CreateClient().PostAsJsonAsync("/api/auth/login",
                new LoginRequest(username, TestUsers.Password));
            response.EnsureSuccessStatusCode();
            token = (await response.Content.ReadFromJsonAsync<LoginResponse>(Json.Options))!.AccessToken;
            _tokens[username] = token;
        }

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting entra antes do Program.cs ler a configuração (Minimal API lê tudo na montagem).
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Jwt:Key", _jwtKey);

        for (var i = 0; i < TestUsers.All.Length; i++)
        {
            builder.UseSetting($"Auth:Users:{i}:Username", TestUsers.All[i].Username);
            builder.UseSetting($"Auth:Users:{i}:PasswordHash", PasswordHash);
            builder.UseSetting($"Auth:Users:{i}:Role", TestUsers.All[i].Role);
        }

        foreach (var (key, value) in RateLimits)
            builder.UseSetting(key, value);
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}

/// <summary>Mesma API, com limites baixos de propósito para provocar o 429.</summary>
public class RateLimitedApiFactory : ApiFactory
{
    public const int Limit = 3;

    protected override IReadOnlyDictionary<string, string> RateLimits => new Dictionary<string, string>
    {
        ["RateLimiting:Login:PermitLimit"] = $"{Limit}",
        ["RateLimiting:IaService:TokenLimit"] = "1000",
        ["RateLimiting:Public:PermitLimit"] = $"{Limit}",
        ["RateLimiting:Default:PermitLimit"] = "1000"
    };
}
