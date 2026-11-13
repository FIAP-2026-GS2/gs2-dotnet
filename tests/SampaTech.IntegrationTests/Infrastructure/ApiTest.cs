using System.Text.Json;
using System.Text.Json.Serialization;

namespace SampaTech.IntegrationTests.Infrastructure;

/// <summary>Todas as classes desta coleção usam a mesma API e o mesmo banco, uma de cada vez.</summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}

/// <summary>Base dos testes: limpa o banco antes de cada teste.</summary>
[Collection(ApiCollection.Name)]
public abstract class ApiTest(ApiFactory factory) : IAsyncLifetime
{
    protected ApiFactory Factory { get; } = factory;

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected Task<HttpClient> AsAsync(string username) => Factory.ClientAsAsync(username);

    protected HttpClient Anonymous() => Factory.CreateClient();
}

/// <summary>Mesmo formato JSON da API: camelCase e enums como texto.</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
