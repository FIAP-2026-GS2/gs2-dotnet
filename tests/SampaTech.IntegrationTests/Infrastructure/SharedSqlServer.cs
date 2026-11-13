using Testcontainers.MsSql;

namespace SampaTech.IntegrationTests.Infrastructure;

/// <summary>
/// Um único SQL Server em container para a execução inteira dos testes (sobe uma vez, ~20 s).
/// Cada fábrica cria o próprio banco dentro dele. O Testcontainers remove o container no final.
/// </summary>
public static class SharedSqlServer
{
    private const string Image = "mcr.microsoft.com/mssql/server:2022-latest";

    private static readonly Lazy<Task<string>> Start = new(async () =>
    {
        var container = new MsSqlBuilder(Image).Build();
        await container.StartAsync();
        return container.GetConnectionString();
    });

    public static Task<string> GetConnectionStringAsync() => Start.Value;
}
