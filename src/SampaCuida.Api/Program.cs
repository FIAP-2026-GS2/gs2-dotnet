using SampaCuida.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { name = "Sampa Cuida API", status = "running" }));

app.Run();

// Torna a classe Program visível para o WebApplicationFactory nos testes de integração.
public partial class Program;
