using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SampaTech.Application.Analyses;
using SampaTech.Application.Captures;
using SampaTech.Application.Common.Paging;
using SampaTech.Application.IssueReports;
using SampaTech.Domain.Enums;
using SampaTech.IntegrationTests.Infrastructure;

namespace SampaTech.IntegrationTests;

/// <summary>O fluxo central: captura → análise da IA → ocorrência → cidadão acompanha.</summary>
public class CaptureFlowTests(ApiFactory factory) : ApiTest(factory)
{
    private static readonly DateTimeOffset Noon = new(2026, 11, 10, 15, 0, 0, TimeSpan.Zero);

    private async Task<CaptureResponse> CaptureAsync(HttpClient ia, int cameraId, int scheduleId, DateTimeOffset date)
    {
        var response = await ia.PostIdempotentAsync("/api/captures", Seed.Capture(cameraId, scheduleId, date));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<CaptureResponse>();
    }

    private async Task<AnalysisResultResponse> AnalyzeAsync(HttpClient ia, int imageId, int issueTypeId, decimal confidence = 0.9m)
    {
        var response = await ia.PostIdempotentAsync("/api/analyses", Seed.Analysis(imageId, issueTypeId, confidence));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<AnalysisResultResponse>();
    }

    [Fact]
    public async Task Deteccao_confiavel_abre_ocorrencia_visivel_para_prefeitura_e_cidadao()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var ia = await AsAsync(TestUsers.IaService);
        var (camera, schedule, issueType) = await admin.CameraWithScheduleAsync();

        var capture = await CaptureAsync(ia, camera.Id, schedule.Id, Noon);
        var result = await AnalyzeAsync(ia, capture.Images[0].Id, issueType.Id);

        var detection = Assert.Single(result.Detections);
        Assert.Equal(DetectionOutcome.OPENED_NEW_ISSUE, detection.Outcome);
        Assert.Equal(CaptureStatus.ANALYZED, result.CaptureStatus);

        var internalList = await (await admin.GetAsync("/api/issue-reports"))
            .ReadAsync<PagedResponse<IssueReportSummaryResponse>>();
        var report = Assert.Single(internalList.Items);
        Assert.Equal(detection.IssueReportId, report.Id);
        Assert.Equal(IssueStatus.OPEN, report.Status);

        var publicList = await (await Anonymous().GetAsync("/api/public/issue-reports"))
            .ReadAsync<PagedResponse<PublicIssueReportResponse>>();
        Assert.Equal(report.Id, Assert.Single(publicList.Items).Id);
    }

    [Fact]
    public async Task Mesmo_problema_visto_de_novo_soma_na_ocorrencia_aberta()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var ia = await AsAsync(TestUsers.IaService);
        var (camera, schedule, issueType) = await admin.CameraWithScheduleAsync();

        var first = await CaptureAsync(ia, camera.Id, schedule.Id, Noon);
        var opened = await AnalyzeAsync(ia, first.Images[0].Id, issueType.Id);

        var second = await CaptureAsync(ia, camera.Id, schedule.Id, Noon.AddDays(1));
        var linked = await AnalyzeAsync(ia, second.Images[0].Id, issueType.Id);

        Assert.Equal(DetectionOutcome.LINKED_TO_EXISTING_ISSUE, linked.Detections[0].Outcome);
        Assert.Equal(opened.Detections[0].IssueReportId, linked.Detections[0].IssueReportId);

        var report = Assert.Single((await (await admin.GetAsync("/api/issue-reports"))
            .ReadAsync<PagedResponse<IssueReportSummaryResponse>>()).Items);
        Assert.Equal(2, report.DetectionCount);
    }

    [Fact]
    public async Task Deteccao_com_confianca_baixa_fica_registrada_sem_abrir_ocorrencia()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var ia = await AsAsync(TestUsers.IaService);
        var (camera, schedule, issueType) = await admin.CameraWithScheduleAsync();

        var capture = await CaptureAsync(ia, camera.Id, schedule.Id, Noon);
        var result = await AnalyzeAsync(ia, capture.Images[0].Id, issueType.Id, confidence: 0.3m);

        Assert.Equal(DetectionOutcome.BELOW_CONFIDENCE_THRESHOLD, result.Detections[0].Outcome);
        Assert.Null(result.Detections[0].IssueReportId);
        Assert.Empty((await (await admin.GetAsync("/api/issue-reports"))
            .ReadAsync<PagedResponse<IssueReportSummaryResponse>>()).Items);
    }

    [Fact]
    public async Task Captura_ja_analisada_nao_e_analisada_de_novo()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var ia = await AsAsync(TestUsers.IaService);
        var (camera, schedule, issueType) = await admin.CameraWithScheduleAsync();
        var capture = await CaptureAsync(ia, camera.Id, schedule.Id, Noon);
        await AnalyzeAsync(ia, capture.Images[0].Id, issueType.Id);

        // Chave nova: não é repetição de envio, é uma segunda análise da mesma captura.
        var response = await ia.PostIdempotentAsync("/api/analyses", Seed.Analysis(capture.Images[0].Id, issueType.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("já foi analisada", (await response.ReadAsync<ProblemDetails>()).Detail);
    }

    [Fact]
    public async Task Imagem_sem_anonimizacao_e_recusada()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var ia = await AsAsync(TestUsers.IaService);
        var (camera, schedule, _) = await admin.CameraWithScheduleAsync();

        var response = await ia.PostIdempotentAsync("/api/captures",
            Seed.Capture(camera.Id, schedule.Id, Noon, anonymized: false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await (await admin.GetAsync("/api/captures")).ReadAsync<PagedResponse<CaptureResponse>>()).Items);
    }

    [Fact]
    public async Task Ocorrencia_rejeitada_some_da_rota_publica()
    {
        var admin = await AsAsync(TestUsers.Admin);
        var ia = await AsAsync(TestUsers.IaService);
        var (camera, schedule, issueType) = await admin.CameraWithScheduleAsync();
        var capture = await CaptureAsync(ia, camera.Id, schedule.Id, Noon);
        var reportId = (await AnalyzeAsync(ia, capture.Images[0].Id, issueType.Id)).Detections[0].IssueReportId;

        var reject = await admin.PatchAsJsonAsync($"/api/issue-reports/{reportId}/status",
            new ChangeIssueStatusRequest(IssueStatus.REJECTED, "Falso positivo: sombra de árvore."), Json.Options);

        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
        Assert.Empty((await (await Anonymous().GetAsync("/api/public/issue-reports"))
            .ReadAsync<PagedResponse<PublicIssueReportResponse>>()).Items);
    }
}
