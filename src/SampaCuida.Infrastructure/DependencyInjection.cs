using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampaCuida.Application.Abstractions;
using SampaCuida.Application.Common.Idempotency;
using SampaCuida.Infrastructure.Idempotency;
using SampaCuida.Infrastructure.Persistence;

namespace SampaCuida.Infrastructure;

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

        return services;
    }
}
