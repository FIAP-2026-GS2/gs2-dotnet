using Azure.Storage.Blobs;
using HealthChecks.Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SampaTech.Infrastructure.Persistence;

namespace SampaTech.Infrastructure.HealthChecks;

/// <summary>Nomes dos checks, como aparecem no JSON do /health.</summary>
public static class HealthCheckNames
{
    public const string Database = "database";
    public const string AiService = "ai-service";
    public const string BlobStorage = "blob-storage";
    public const string RabbitMq = "rabbitmq";
}

/// <summary>Tag dos checks de dependência: só o /health os executa (o /health/live não toca em nada).</summary>
public static class HealthCheckTags
{
    public const string Ready = "ready";
}

public static class HealthCheckSetup
{
    public const string SectionName = "HealthChecks";
    public const int DefaultTimeoutSeconds = 3;

    public static IServiceCollection AddInfrastructureHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string[] ready = [HealthCheckTags.Ready];

        // Sem banco a API não funciona: se cair, o /health fica Unhealthy (503).
        // Com o servidor fora do ar, o SqlClient abre a conexão de forma síncrona e ignora o cancelamento
        // (esperaria o Connect Timeout inteiro, 15 s por padrão). Por isso a tentativa roda em outra thread
        // (Task.Run) e o WaitAsync faz o check desistir no tempo-limite do health check.
        var checks = services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(
                name: HealthCheckNames.Database,
                failureStatus: HealthStatus.Unhealthy,
                tags: ready,
                customTestQuery: (db, cancellationToken) =>
                    Task.Run(() => db.Database.CanConnectAsync(cancellationToken), cancellationToken)
                        .WaitAsync(cancellationToken));

        // Os serviços externos só entram se estiverem configurados. Se caírem, a API continua
        // respondendo (cadastros, ocorrências), então o resultado é Degraded (200), não Unhealthy.
        if (configuration["AiService:BaseUrl"] is { Length: > 0 } aiBaseUrl)
        {
            checks.AddUrlGroup(
                uri: AiHealthUri(aiBaseUrl),
                name: HealthCheckNames.AiService,
                failureStatus: HealthStatus.Degraded,
                tags: ready);
        }

        if (configuration.GetConnectionString("BlobStorage") is { Length: > 0 } blobConnectionString)
        {
            var containerName = configuration["BlobStorage:ContainerName"] ?? "frames";
            checks.AddAzureBlobStorage(
                clientFactory: _ => new BlobServiceClient(blobConnectionString),
                optionsFactory: _ => new AzureBlobStorageHealthCheckOptions { ContainerName = containerName },
                name: HealthCheckNames.BlobStorage,
                failureStatus: HealthStatus.Degraded,
                tags: ready);
        }

        if (configuration.GetConnectionString("RabbitMq") is { Length: > 0 } rabbitConnectionString)
        {
            checks.AddRabbitMQ(
                rabbitConnectionString: rabbitConnectionString,
                name: HealthCheckNames.RabbitMq,
                failureStatus: HealthStatus.Degraded,
                tags: ready);
        }

        // Um único tempo-limite para todos os checks: um serviço lento não pode travar o /health.
        // Estourou o tempo, o check conta como falho (com o failureStatus dele).
        var timeoutSeconds = configuration.GetValue($"{SectionName}:TimeoutSeconds", DefaultTimeoutSeconds);
        services.PostConfigure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations)
                registration.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        return services;
    }

    // "https://ia.exemplo.com" ou "https://ia.exemplo.com/" viram "https://ia.exemplo.com/health".
    private static Uri AiHealthUri(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException(
                "AiService:BaseUrl precisa ser uma URL http(s) completa, por exemplo https://ia.exemplo.com.");

        return new Uri(baseUri, "health");
    }
}
