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

        return services;
    }
}
