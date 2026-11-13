using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SampaTech.Application.Captures;
using SampaTech.Application.Common.Paging;
using SampaTech.IntegrationTests.Infrastructure;

namespace SampaTech.IntegrationTests;

public class IdempotencyTests(ApiFactory factory) : ApiTest(factory)
{
    private static readonly DateTimeOffset Noon = new(2026, 11, 10, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Reenvio_com_a_mesma_chave_devolve_a_resposta_original_sem_gravar_de_novo()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var ia = await AsAsync(TestUsers.IaService);
        var (camera, schedule, _) = await admin.CameraWithScheduleAsync();
        var body = Seed.Capture(camera.Id, schedule.Id, Noon);

        var first = await ia.PostIdempotentAsync("/api/captures", body, "captura-teste-1");
        var second = await ia.PostIdempotentAsync("/api/captures", body, "captura-teste-1");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.False(first.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal("true", Assert.Single(second.Headers.GetValues("Idempotency-Replayed")));
        Assert.Equal((await first.ReadAsync<CaptureResponse>()).Id, (await second.ReadAsync<CaptureResponse>()).Id);

        var captures = await (await admin.GetAsync("/api/captures")).ReadAsync<PagedResponse<CaptureResponse>>();
        Assert.Equal(1, captures.TotalItems);
    }

    [Fact]
    public async Task Mesma_chave_com_outro_conteudo_responde_422()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var ia = await AsAsync(TestUsers.IaService);
        var (camera, schedule, _) = await admin.CameraWithScheduleAsync();

        await ia.PostIdempotentAsync("/api/captures", Seed.Capture(camera.Id, schedule.Id, Noon), "captura-teste-2");
        var response = await ia.PostIdempotentAsync("/api/captures",
            Seed.Capture(camera.Id, schedule.Id, Noon.AddDays(1)), "captura-teste-2");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Sem_a_chave_responde_400_explicando_o_header()
    {
        var ia = await AsAsync(TestUsers.IaService);

        var response = await ia.PostAsJsonAsync("/api/captures", Seed.Capture(1, 1, Noon), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Idempotency-Key", (await response.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task Mesma_camera_e_horario_com_chave_nova_e_barrada_pelo_indice_unico()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var ia = await AsAsync(TestUsers.IaService);
        var (camera, schedule, _) = await admin.CameraWithScheduleAsync();
        var body = Seed.Capture(camera.Id, schedule.Id, Noon);

        await ia.PostIdempotentAsync("/api/captures", body, "captura-teste-3");
        var duplicate = await ia.PostIdempotentAsync("/api/captures", body, "captura-teste-4");

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }
}
