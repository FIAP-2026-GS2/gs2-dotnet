using System.Globalization;
using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SampaCuida.Application.Analyses;
using SampaCuida.Application.Cameras;
using SampaCuida.Application.Captures;
using SampaCuida.Application.CaptureSchedules;
using SampaCuida.Application.IssueReports;
using SampaCuida.Application.IssueTypes;
using SampaCuida.Application.MonitoringPoints;
using SampaCuida.Application.MunicipalAgencies;
using SampaCuida.Application.Schedules;
using SampaCuida.Application.Teams;
using SampaCuida.Application.TriggerEvents;

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
        services.AddScoped<IMunicipalAgencyService, MunicipalAgencyService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IIssueTypeService, IssueTypeService>();
        services.AddScoped<ICaptureScheduleService, CaptureScheduleService>();
        services.AddScoped<ITriggerEventService, TriggerEventService>();
        services.AddScoped<ICaptureService, CaptureService>();
        services.AddScoped<IAnalysisService, AnalysisService>();
        services.AddScoped<IDueScheduleService, DueScheduleService>();
        services.AddScoped<IIssueReportService, IssueReportService>();

        return services;
    }
}
