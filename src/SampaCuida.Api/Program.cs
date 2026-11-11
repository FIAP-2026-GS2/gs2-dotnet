using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using SampaCuida.Api.Endpoints;
using SampaCuida.Api.Errors;
using SampaCuida.Api.Idempotency;
using SampaCuida.Application;
using SampaCuida.Application.Analyses;
using SampaCuida.Application.Schedules;
using SampaCuida.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Regras configuráveis (appsettings): confiança mínima para uma detecção abrir ocorrência.
builder.Services.Configure<DetectionOptions>(builder.Configuration.GetSection(DetectionOptions.SectionName));

// Agenda: tolerância da janela, intervalo dos frames de chuva e fuso das regras de relógio.
builder.Services.Configure<ScheduleOptions>(builder.Configuration.GetSection(ScheduleOptions.SectionName));

// Erros padronizados (ProblemDetails) a partir de um único tratador global.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Enums como texto no JSON ("ACTIVE" em vez de 0), na API e no Swagger.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// "Relógio" injetável: nos testes dá para fixar o "agora".
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sampa Cuida API",
        Version = "v1",
        Description = "Zeladoria urbana a partir das câmeras do Smart Sampa: câmeras, agendas de captura, " +
                      "detecções da IA e ocorrências acompanhadas pela prefeitura e pelo cidadão."
    });

    // Mostra o header Idempotency-Key nas rotas idempotentes.
    options.OperationFilter<IdempotencyHeaderOperationFilter>();
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Swagger sempre em Development; em produção (Azure), só com Swagger__Enabled=true.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "Sampa Cuida API");
}

app.MapGet("/", () => Results.Ok(new { name = "Sampa Cuida API", status = "running" }))
    .ExcludeFromDescription();

app.MapMonitoringPointEndpoints();
app.MapCameraEndpoints();
app.MapCaptureScheduleEndpoints();
app.MapMunicipalAgencyEndpoints();
app.MapTeamEndpoints();
app.MapIssueTypeEndpoints();
app.MapTriggerEventEndpoints();
app.MapCaptureEndpoints();
app.MapAnalysisEndpoints();
app.MapDueScheduleEndpoints();
app.MapIssueReportEndpoints();

app.Run();

// Torna a classe Program visível para o WebApplicationFactory nos testes de integração.
public partial class Program;
