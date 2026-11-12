using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using SampaCuida.Api.Auth;
using SampaCuida.Api.Endpoints;
using SampaCuida.Api.Errors;
using SampaCuida.Api.Idempotency;
using SampaCuida.Api.RateLimiting;
using SampaCuida.Application;
using SampaCuida.Application.Analyses;
using SampaCuida.Application.Schedules;
using SampaCuida.Infrastructure;
using SampaCuida.Infrastructure.Auth;

// Ferramenta de linha de comando para gerar o hash de uma senha (a senha nunca fica em texto):
//   dotnet run --project src/SampaCuida.Api -- hash-password 'minha senha'
if (args is ["hash-password", var password])
{
    Console.WriteLine(PasswordHashing.Hash(password));
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Regras configuráveis (appsettings): confiança mínima para uma detecção abrir ocorrência.
builder.Services.Configure<DetectionOptions>(builder.Configuration.GetSection(DetectionOptions.SectionName));

// Agenda: tolerância da janela, intervalo dos frames de chuva e fuso das regras de relógio.
builder.Services.Configure<ScheduleOptions>(builder.Configuration.GetSection(ScheduleOptions.SectionName));

// JWT: validação do token, papéis em escada (ADMIN › MANAGER › VIEWER, e IA_SERVICE) e proteção por padrão.
builder.Services.AddJwtAuthentication(builder.Configuration);

// Limite de requisições (429): login, serviço de IA, rota pública e um limite geral para o resto.
builder.Services.AddApiRateLimiting(builder.Configuration);

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
                      "detecções da IA e ocorrências acompanhadas pela prefeitura e pelo cidadão. " +
                      "Faça login em POST /api/auth/login e cole o token no botão Authorize."
    });

    // Botão "Authorize" para colar o token, e cadeado só nas rotas protegidas.
    options.AddSecurityDefinition(AuthorizeOperationFilter.SchemeName, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Cole só o token (sem a palavra Bearer)."
    });
    options.OperationFilter<AuthorizeOperationFilter>();

    // Mostra o header Idempotency-Key nas rotas idempotentes.
    options.OperationFilter<IdempotencyHeaderOperationFilter>();

    // Documenta o 429 nas rotas com limite de requisições.
    options.OperationFilter<TooManyRequestsOperationFilter>();
});

var app = builder.Build();

// Primeiro de tudo: lê o IP real do cliente quando a API está atrás do balanceador do Azure.
app.UseForwardedHeaders();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Swagger sempre em Development; em produção (Azure), só com Swagger__Enabled=true.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "Sampa Cuida API");
}

// Ordem importa: autenticar antes de limitar (a cota é por usuário) e limitar antes de autorizar.
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { name = "Sampa Cuida API", status = "running" }))
    .ExcludeFromDescription()
    .AllowAnonymous();

app.MapAuthEndpoints();
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
