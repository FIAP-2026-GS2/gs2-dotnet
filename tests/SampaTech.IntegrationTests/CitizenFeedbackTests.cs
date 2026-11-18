using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SampaTech.Application.CitizenFeedbacks;
using SampaTech.Application.Common.Paging;
using SampaTech.Application.IssueReports;
using SampaTech.Application.MunicipalAgencies;
using SampaTech.Application.Teams;
using SampaTech.Domain.Enums;
using SampaTech.IntegrationTests.Infrastructure;

namespace SampaTech.IntegrationTests;

/// <summary>Cidadão confirma ou contesta o conserto com foto; a equipe aprova ou recusa.</summary>
public class CitizenFeedbackTests(ApiFactory factory) : ApiTest(factory)
{
    private static readonly DateTimeOffset Noon = new(2026, 11, 10, 15, 0, 0, TimeSpan.Zero);

    // Começo de um JPEG de verdade (a API confere a "assinatura" do arquivo).
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];

    /// <summary>Ocorrência aberta pela IA e levada até RESOLVED pela equipe.</summary>
    private async Task<int> ResolvedIssueAsync(bool resolve = true)
    {
        var admin = await AsAsync(TestUsers.Admin);
        var ia = await AsAsync(TestUsers.IaService);
        var (camera, schedule, issueType) = await admin.CameraWithScheduleAsync();

        var capture = await (await ia.PostIdempotentAsync("/api/captures", Seed.Capture(camera.Id, schedule.Id, Noon)))
            .ReadAsync<Application.Captures.CaptureResponse>();
        var analysis = await (await ia.PostIdempotentAsync("/api/analyses", Seed.Analysis(capture.Images[0].Id, issueType.Id)))
            .ReadAsync<Application.Analyses.AnalysisResultResponse>();
        var reportId = analysis.Detections[0].IssueReportId!.Value;

        if (!resolve)
            return reportId;

        var agency = await admin.PostCreatedAsync<MunicipalAgencyResponse>("/api/municipal-agencies",
            new MunicipalAgencyRequest("Subprefeitura Sé", "São Paulo", "SP", null, "Sé"));
        var team = await admin.PostCreatedAsync<TeamResponse>("/api/teams", new TeamRequest("Tapa-buraco", agency.Id, null));

        await admin.PostAsJsonAsync($"/api/issue-reports/{reportId}/assignments", new CreateAssignmentRequest(team.Id), Json.Options);
        await admin.PatchAsJsonAsync($"/api/issue-reports/{reportId}/status",
            new ChangeIssueStatusRequest(IssueStatus.IN_PROGRESS, null), Json.Options);
        var resolved = await admin.PatchAsJsonAsync($"/api/issue-reports/{reportId}/status",
            new ChangeIssueStatusRequest(IssueStatus.RESOLVED, "Buraco tapado"), Json.Options);
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);

        return reportId;
    }

    private static Task<HttpResponseMessage> SendFeedbackAsync(
        HttpClient client, int issueId, string decision = "CONFIRM", string? notes = null,
        byte[]? photo = null, string? key = null, bool withPhoto = true)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(decision), "decision" },
            { new StringContent("-23.5558"), "latitude" },
            { new StringContent("-46.6623"), "longitude" }
        };
        if (notes is not null)
            form.Add(new StringContent(notes), "notes");
        if (withPhoto)
        {
            var file = new ByteArrayContent(photo ?? Jpeg);
            file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(file, "photo", "foto.jpg");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/issue-reports/{issueId}/citizen-feedback") { Content = form };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }

    private async Task<CitizenFeedbackResponse> ReviewAsync(int feedbackId, bool approved, string? notes = null)
    {
        var manager = await AsAsync(TestUsers.Manager);
        var response = await manager.PatchAsJsonAsync($"/api/citizen-feedback/{feedbackId}/review",
            new ReviewCitizenFeedbackRequest(approved, notes), Json.Options);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<CitizenFeedbackResponse>();
    }

    [Fact]
    public async Task Cidadao_manda_foto_e_o_relato_aguarda_a_equipe()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);

        var response = await SendFeedbackAsync(citizen, issueId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var feedback = await response.ReadAsync<CitizenFeedbackResponse>();
        Assert.Equal($"/api/citizen-feedback/{feedback.Id}", response.Headers.Location!.ToString());
        Assert.Equal(CitizenFeedbackStatus.PENDING, feedback.Status);
        Assert.Equal("Foto recebida e validada pela IA. Aguardamos a aprovação da equipe para confirmar o relato.",
            feedback.Message);
        Assert.StartsWith("https://teste.blob.core.windows.net/citizen-photos/", feedback.PhotoUrl);
        Assert.Single(Factory.Photos.StoredFiles);
    }

    [Fact]
    public async Task Confirmacao_aprovada_vira_validada_por_cidadao_na_rota_publica()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);
        var sent = await (await SendFeedbackAsync(citizen, issueId)).ReadAsync<CitizenFeedbackResponse>();

        var reviewed = await ReviewAsync(sent.Id, approved: true);

        Assert.Equal("Ocorrência resolvida e validada por 1 cidadão(s).", reviewed.Message);
        var seenByCitizen = await (await citizen.GetAsync($"/api/citizen-feedback/{sent.Id}")).ReadAsync<CitizenFeedbackResponse>();
        Assert.Equal(reviewed.Message, seenByCitizen.Message);

        var publicItem = Assert.Single((await (await Anonymous().GetAsync("/api/public/issue-reports"))
            .ReadAsync<PagedResponse<PublicIssueReportResponse>>()).Items);
        Assert.Equal(IssueStatus.RESOLVED, publicItem.Status);
        Assert.Equal(1, publicItem.CitizenConfirmations);
    }

    [Fact]
    public async Task Contestacao_aprovada_reabre_a_ocorrencia()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);
        var sent = await (await SendFeedbackAsync(citizen, issueId, "CONTEST", "O buraco continua lá")).ReadAsync<CitizenFeedbackResponse>();

        var reviewed = await ReviewAsync(sent.Id, approved: true);

        Assert.Equal("Contestada e não resolvida: a ocorrência foi reaberta para a equipe.", reviewed.Message);
        var admin = await AsAsync(TestUsers.Admin);
        var detail = await (await admin.GetAsync($"/api/issue-reports/{issueId}")).ReadAsync<IssueReportDetailResponse>();
        Assert.Equal(IssueStatus.OPEN, detail.Summary.Status);
        Assert.Null(detail.Summary.ResolutionDate);
        Assert.Contains("contestou", detail.History.Last().Notes);
        Assert.Equal(1, detail.Summary.CitizenContests);
    }

    [Fact]
    public async Task Recusa_exige_motivo_e_relato_revisado_nao_volta()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);
        var manager = await AsAsync(TestUsers.Manager);
        var sent = await (await SendFeedbackAsync(citizen, issueId)).ReadAsync<CitizenFeedbackResponse>();

        var withoutReason = await manager.PatchAsJsonAsync($"/api/citizen-feedback/{sent.Id}/review",
            new ReviewCitizenFeedbackRequest(false, null), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, withoutReason.StatusCode);

        var rejected = await ReviewAsync(sent.Id, approved: false, "Foto de outra rua");
        Assert.Equal("O relato não foi aprovado pela equipe: Foto de outra rua", rejected.Message);

        var again = await manager.PatchAsJsonAsync($"/api/citizen-feedback/{sent.Id}/review",
            new ReviewCitizenFeedbackRequest(true, null), Json.Options);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Ocorrencia_que_nao_esta_resolvida_responde_409()
    {
        var issueId = await ResolvedIssueAsync(resolve: false);
        var citizen = await AsAsync(TestUsers.Citizen);

        var response = await SendFeedbackAsync(citizen, issueId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("resolvida", (await response.ReadAsync<ProblemDetails>()).Detail);
    }

    [Fact]
    public async Task So_o_cidadao_envia_foto()
    {
        var issueId = await ResolvedIssueAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await SendFeedbackAsync(Anonymous(), issueId)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendFeedbackAsync(await AsAsync(TestUsers.Manager), issueId)).StatusCode);
    }

    [Fact]
    public async Task Dados_invalidos_respondem_400_com_os_campos()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);

        var withoutPhoto = await SendFeedbackAsync(citizen, issueId, "CONTEST", withPhoto: false);

        Assert.Equal(HttpStatusCode.BadRequest, withoutPhoto.StatusCode);
        var errors = (await withoutPhoto.ReadAsync<ValidationProblemDetails>()).Errors;
        Assert.Contains("photo", errors.Keys);
        Assert.Contains("notes", errors.Keys);
    }

    [Fact]
    public async Task Arquivo_que_nao_e_imagem_responde_400()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);

        var response = await SendFeedbackAsync(citizen, issueId, photo: "%PDF-1.7 não é foto"u8.ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("JPG ou PNG", (await response.ReadAsync<ProblemDetails>()).Detail);
        Assert.Empty(Factory.Photos.StoredFiles);
    }

    [Fact]
    public async Task Foto_acima_de_5_MB_responde_413()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);
        var big = new byte[5 * 1024 * 1024 + 1];
        Jpeg.CopyTo(big, 0);

        var response = await SendFeedbackAsync(citizen, issueId, photo: big);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task IA_fora_do_ar_responde_503_e_nada_e_guardado()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);
        Factory.Photos.AiAvailable = false;

        var response = await SendFeedbackAsync(citizen, issueId);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Empty(Factory.Photos.StoredFiles);
        var manager = await AsAsync(TestUsers.Manager);
        Assert.Equal(0, (await (await manager.GetAsync("/api/citizen-feedback"))
            .ReadAsync<PagedResponse<CitizenFeedbackResponse>>()).TotalItems);
    }

    [Fact]
    public async Task Foto_recusada_pela_IA_responde_422_com_o_motivo_e_nada_e_guardado()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);
        Factory.Photos.RejectionReason = "A foto não mostra uma via pública: um pássaro em um galho.";

        var response = await SendFeedbackAsync(citizen, issueId, "CONTEST", "O buraco continua lá");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>();
        Assert.Equal("Foto recusada pela IA", problem.Title);
        Assert.Equal("A foto não mostra uma via pública: um pássaro em um galho.", problem.Detail);
        Assert.Empty(Factory.Photos.StoredFiles);
        var manager = await AsAsync(TestUsers.Manager);
        Assert.Equal(0, (await (await manager.GetAsync("/api/citizen-feedback"))
            .ReadAsync<PagedResponse<CitizenFeedbackResponse>>()).TotalItems);
    }

    [Fact]
    public async Task A_IA_recebe_o_tipo_do_problema_e_a_decisao_do_cidadao()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);

        await SendFeedbackAsync(citizen, issueId, "CONTEST", "O buraco continua lá");

        // A ocorrência de teste é de buraco (tipo criado pelo Seed).
        Assert.Equal((IssueCategory.BURACO, CitizenFeedbackDecision.CONTEST), Factory.Photos.LastCheck);
    }

    [Fact]
    public async Task Reenvio_com_a_mesma_chave_nao_duplica_o_relato()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);

        var first = await SendFeedbackAsync(citizen, issueId, key: "relato-1");
        var second = await SendFeedbackAsync(citizen, issueId, key: "relato-1");

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal("true", Assert.Single(second.Headers.GetValues("Idempotency-Replayed")));
        Assert.Equal((await first.ReadAsync<CitizenFeedbackResponse>()).Id, (await second.ReadAsync<CitizenFeedbackResponse>()).Id);
        Assert.Single(Factory.Photos.StoredFiles);
    }

    [Fact]
    public async Task Equipe_ve_a_foto_borrada_para_decidir()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);
        var sent = await (await SendFeedbackAsync(citizen, issueId)).ReadAsync<CitizenFeedbackResponse>();

        var viewer = await AsAsync(TestUsers.Viewer);
        var photo = await viewer.GetAsync($"/api/citizen-feedback/{sent.Id}/photo");

        Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
        Assert.Equal("image/jpeg", photo.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Jpeg, await photo.Content.ReadAsByteArrayAsync());

        // O próprio cidadão também vê a foto que mandou; sem login, não.
        Assert.Equal(HttpStatusCode.OK, (await citizen.GetAsync($"/api/citizen-feedback/{sent.Id}/photo")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous().GetAsync($"/api/citizen-feedback/{sent.Id}/photo")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await viewer.GetAsync("/api/citizen-feedback/999/photo")).StatusCode);
    }

    [Fact]
    public async Task Equipe_ve_a_fila_de_relatos_pendentes()
    {
        var issueId = await ResolvedIssueAsync();
        var citizen = await AsAsync(TestUsers.Citizen);
        await SendFeedbackAsync(citizen, issueId);

        var viewer = await AsAsync(TestUsers.Viewer);
        var queue = await (await viewer.GetAsync("/api/citizen-feedback?status=PENDING"))
            .ReadAsync<PagedResponse<CitizenFeedbackResponse>>();

        Assert.Equal(issueId, Assert.Single(queue.Items).IssueReportId);
        Assert.Equal(HttpStatusCode.Forbidden, (await citizen.GetAsync("/api/citizen-feedback")).StatusCode);
    }
}
