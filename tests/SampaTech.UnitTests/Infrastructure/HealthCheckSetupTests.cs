using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SampaTech.Infrastructure.HealthChecks;

namespace SampaTech.UnitTests.Infrastructure;

public class HealthCheckSetupTests
{
    private static IReadOnlyList<HealthCheckRegistration> Registrations(Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build();
        var services = new ServiceCollection();
        services.AddInfrastructureHealthChecks(configuration);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.ToList();
    }

    private static readonly Dictionary<string, string?> AllConfigured = new()
    {
        ["AiService:BaseUrl"] = "https://ia.exemplo.com",
        ["ConnectionStrings:BlobStorage"] = "UseDevelopmentStorage=true",
        ["ConnectionStrings:RabbitMq"] = "amqp://guest:guest@localhost:5672/"
    };

    [Fact]
    public void Sem_servicos_configurados_so_verifica_o_banco()
    {
        var registration = Assert.Single(Registrations());

        Assert.Equal(HealthCheckNames.Database, registration.Name);
        Assert.Equal(HealthStatus.Unhealthy, registration.FailureStatus);
        Assert.Contains(HealthCheckTags.Ready, registration.Tags);
    }

    [Fact]
    public void Servicos_configurados_entram_como_Degraded()
    {
        var registrations = Registrations(AllConfigured);

        Assert.Equal(
            [HealthCheckNames.Database, HealthCheckNames.AiService, HealthCheckNames.BlobStorage, HealthCheckNames.RabbitMq],
            registrations.Select(r => r.Name));
        Assert.All(registrations.Where(r => r.Name != HealthCheckNames.Database),
            r => Assert.Equal(HealthStatus.Degraded, r.FailureStatus));
        Assert.All(registrations, r => Assert.Contains(HealthCheckTags.Ready, r.Tags));
    }

    [Theory]
    [InlineData("AiService:BaseUrl", HealthCheckNames.AiService)]
    [InlineData("ConnectionStrings:BlobStorage", HealthCheckNames.BlobStorage)]
    [InlineData("ConnectionStrings:RabbitMq", HealthCheckNames.RabbitMq)]
    public void Configuracao_vazia_nao_registra_o_check(string key, string name)
    {
        var registrations = Registrations(new() { [key] = "" });

        Assert.DoesNotContain(registrations, r => r.Name == name);
    }

    [Fact]
    public void Tempo_limite_padrao_e_3_segundos_para_todos()
    {
        Assert.All(Registrations(AllConfigured), r => Assert.Equal(TimeSpan.FromSeconds(3), r.Timeout));
    }

    [Fact]
    public void Tempo_limite_vem_da_configuracao()
    {
        var settings = new Dictionary<string, string?>(AllConfigured) { ["HealthChecks:TimeoutSeconds"] = "5" };

        Assert.All(Registrations(settings), r => Assert.Equal(TimeSpan.FromSeconds(5), r.Timeout));
    }

    [Theory]
    [InlineData("ia.exemplo.com")]
    [InlineData("ftp://ia.exemplo.com")]
    public void Url_do_servico_de_IA_invalida_impede_a_subida(string baseUrl)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => Registrations(new() { ["AiService:BaseUrl"] = baseUrl }));

        Assert.Contains("AiService:BaseUrl", exception.Message);
    }
}
