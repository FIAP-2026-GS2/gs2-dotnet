using System.Globalization;
using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SampaCuida.Application.Cameras;
using SampaCuida.Application.MonitoringPoints;

namespace SampaCuida.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Mensagens padrão do FluentValidation em português, e chaves dos erros
        // em camelCase, iguais aos nomes dos campos no JSON.
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");
        ValidatorOptions.Global.PropertyNameResolver = (_, member, _) =>
            member is null ? null : JsonNamingPolicy.CamelCase.ConvertName(member.Name);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IMonitoringPointService, MonitoringPointService>();
        services.AddScoped<ICameraService, CameraService>();

        return services;
    }
}
