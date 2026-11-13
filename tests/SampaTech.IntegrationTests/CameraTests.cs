using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SampaTech.Application.Cameras;
using SampaTech.Application.Common.Paging;
using SampaTech.Domain.Enums;
using SampaTech.IntegrationTests.Infrastructure;

namespace SampaTech.IntegrationTests;

public class CameraTests(ApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Cadastra_e_busca_a_camera()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var point = await admin.MonitoringPointAsync();

        var response = await admin.PostAsJsonAsync("/api/cameras",
            new CreateCameraRequest("CAM-001", point.Id, "Esquina com a Paulista", null), Json.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.ReadAsync<CameraResponse>();
        Assert.Equal($"/api/cameras/{created.Id}", response.Headers.Location!.ToString());
        Assert.Equal(CameraStatus.ACTIVE, created.Status);

        var found = await (await admin.GetAsync($"/api/cameras/{created.Id}")).ReadAsync<CameraResponse>();
        Assert.Equal(created, found);
    }

    [Fact]
    public async Task Lista_paginada_com_total()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var point = await admin.MonitoringPointAsync();
        await admin.CameraAsync(point.Id, "CAM-001");
        await admin.CameraAsync(point.Id, "CAM-002");
        await admin.CameraAsync(point.Id, "CAM-003");

        var page = await (await admin.GetAsync("/api/cameras?page=1&pageSize=2"))
            .ReadAsync<PagedResponse<CameraResponse>>();

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task Camera_inexistente_responde_404()
    {
        var admin = await AsAsync(TestUsers.Admin);

        var response = await admin.GetAsync("/api/cameras/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Codigo_repetido_responde_409()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var point = await admin.MonitoringPointAsync();
        await admin.CameraAsync(point.Id, "CAM-001");

        var response = await admin.PostAsJsonAsync("/api/cameras",
            new CreateCameraRequest("CAM-001", point.Id, null, null), Json.Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Dados_invalidos_respondem_400_com_o_campo_e_a_mensagem_em_portugues()
    {
        var admin = await AsAsync(TestUsers.Admin);

        var response = await admin.PostAsJsonAsync("/api/cameras",
            new CreateCameraRequest("", 0, null, null), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Contains("code", problem.Errors.Keys);
        Assert.Contains("monitoringPointId", problem.Errors.Keys);
    }

    [Fact]
    public async Task Admin_apaga_a_camera_sem_historico()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var point = await admin.MonitoringPointAsync();
        var camera = await admin.CameraAsync(point.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/cameras/{camera.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/cameras/{camera.Id}")).StatusCode);
    }

    [Fact]
    public async Task Camera_com_agenda_nao_pode_ser_apagada()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var (camera, _, _) = await admin.CameraWithScheduleAsync();

        var response = await admin.DeleteAsync($"/api/cameras/{camera.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>();
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
    }
}
