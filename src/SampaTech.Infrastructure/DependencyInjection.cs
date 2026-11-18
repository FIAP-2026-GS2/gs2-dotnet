using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SampaTech.Application.Abstractions;
using SampaTech.Application.Auth;
using SampaTech.Infrastructure.Auth;
using SampaTech.Application.Common.Idempotency;
using SampaTech.Infrastructure.Idempotency;
using SampaTech.Infrastructure.Persistence;
using SampaTech.Application.Common.Photos;
using SampaTech.Infrastructure.Photos;

namespace SampaTech.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A connection string 'DefaultConnection' não foi configurada.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        // O Application pede IAppDbContext e recebe o mesmo AppDbContext da requisição.
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();

        // Login e token: a chave e os usuários vêm do user-secrets (local) ou do ambiente (Azure).
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        // Os usuários são conferidos na subida: papel inválido, hash vazio ou nome repetido impedem a API de iniciar.
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();
        services.AddSingleton<IAuthService, JwtAuthService>();

        // Fotos dos cidadãos: a IA borra (HTTP, com tempo-limite) e o Blob guarda.
        services.Configure<CitizenPhotoOptions>(configuration.GetSection(CitizenPhotoOptions.SectionName));
        // 60 s: a IA borra a foto e ainda consulta o LLM de visão; se o principal falhar (ex.: limite 429),
        // o provedor reserva responde, e no ensaio real isso levou até ~37 s.
        services.AddHttpClient<IPhotoVerifier, HttpPhotoVerifier>(client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddSingleton<IPhotoStorage, BlobPhotoStorage>();

        return services;
    }
}
