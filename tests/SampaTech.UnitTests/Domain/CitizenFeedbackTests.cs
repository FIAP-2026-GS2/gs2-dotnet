using SampaTech.Application.Common.Photos;
using SampaTech.Domain.Entities;
using SampaTech.Domain.Enums;
using SampaTech.Domain.Exceptions;

namespace SampaTech.UnitTests.Domain;

public class CitizenFeedbackTests
{
    private static readonly DateTime Opened = new(2026, 11, 10, 15, 5, 0, DateTimeKind.Utc);
    private static readonly DateTime Sent = Opened.AddDays(3);

    private static IssueReport CreateResolvedReport()
    {
        var analysis = new AiAnalysis(1, "yolov8-pothole", AnalysisStatus.COMPLETED, Opened);
        var detection = analysis.AddDetection(2, 0.9m, Severity.HIGH);
        var report = IssueReport.FromDetection(detection, "Buraco na Rua Augusta", Opened);
        report.AssignTo(teamId: 4, Opened.AddHours(1));
        report.ChangeStatus(IssueStatus.IN_PROGRESS, Opened.AddHours(2));
        report.ChangeStatus(IssueStatus.RESOLVED, Opened.AddDays(1), "Buraco tapado");
        return report;
    }

    private static Capture CitizenCapture() => Capture.FromCitizen("cidadao-demo", -23.55m, -46.66m, Sent);

    private static CitizenFeedback Create(
        CitizenFeedbackDecision decision = CitizenFeedbackDecision.CONFIRM, string? notes = null, IssueReport? report = null) =>
        new(report ?? CreateResolvedReport(), CitizenCapture(), "cidadao-demo", decision, notes, Sent);

    [Fact]
    public void Relato_nasce_pendente_com_o_cidadao_e_a_decisao()
    {
        var feedback = Create(CitizenFeedbackDecision.CONFIRM);

        Assert.Equal(CitizenFeedbackStatus.PENDING, feedback.Status);
        Assert.Equal(CitizenFeedbackDecision.CONFIRM, feedback.Decision);
        Assert.Equal("cidadao-demo", feedback.UserId);
        Assert.Null(feedback.ReviewedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Contestar_exige_dizer_o_que_continua_errado(string? notes)
    {
        var exception = Assert.Throws<DomainException>(() => Create(CitizenFeedbackDecision.CONTEST, notes));

        Assert.Contains("contestar", exception.Message);
    }

    [Fact]
    public void So_ocorrencia_resolvida_recebe_relato()
    {
        var analysis = new AiAnalysis(1, "yolo", AnalysisStatus.COMPLETED, Opened);
        var open = IssueReport.FromDetection(analysis.AddDetection(2, 0.9m, Severity.HIGH), "Buraco", Opened);

        var exception = Assert.Throws<DomainException>(() => Create(report: open));

        Assert.Contains("resolvida", exception.Message);
    }

    [Fact]
    public void Aprovar_registra_quem_revisou_e_quando()
    {
        var feedback = Create();

        feedback.Approve(Sent.AddHours(1), "gestor-matt", null);

        Assert.Equal(CitizenFeedbackStatus.APPROVED, feedback.Status);
        Assert.Equal("gestor-matt", feedback.ReviewedBy);
        Assert.Equal(Sent.AddHours(1), feedback.ReviewedAt);
    }

    [Fact]
    public void Recusar_exige_motivo()
    {
        var feedback = Create();

        Assert.Throws<DomainException>(() => feedback.Reject(Sent.AddHours(1), "gestor-matt", " "));
        feedback.Reject(Sent.AddHours(1), "gestor-matt", "Foto de outra rua");

        Assert.Equal(CitizenFeedbackStatus.REJECTED, feedback.Status);
        Assert.Equal("Foto de outra rua", feedback.ReviewNotes);
    }

    [Fact]
    public void Relato_revisado_nao_e_revisado_de_novo()
    {
        var feedback = Create();
        feedback.Approve(Sent.AddHours(1), "gestor-matt", null);

        Assert.Throws<DomainException>(() => feedback.Reject(Sent.AddHours(2), "gestor-matt", "Mudei de ideia"));
    }

    [Fact]
    public void Contestacao_aprovada_reabre_a_ocorrencia_e_limpa_a_data_de_resolucao()
    {
        var report = CreateResolvedReport();

        report.ReopenByCitizen(Sent.AddHours(1), "Reaberta: cidadão contestou", "gestor-matt");

        Assert.Equal(IssueStatus.OPEN, report.Status);
        Assert.Null(report.ResolutionDate);
        var last = report.History.Last();
        Assert.Equal(IssueStatus.RESOLVED, last.PreviousStatus);
        Assert.Equal(IssueStatus.OPEN, last.NewStatus);
        Assert.Equal("gestor-matt", last.UserId);

        // Reaberta, volta para a fila: dá para atribuir outra equipe.
        report.AssignTo(teamId: 5, Sent.AddHours(2));
        Assert.Equal(IssueStatus.ASSIGNED, report.Status);
    }

    [Fact]
    public void Confirmacao_aprovada_fica_no_historico_sem_mudar_o_status()
    {
        var report = CreateResolvedReport();

        report.RecordCitizenConfirmation(Sent.AddHours(1), "Conserto confirmado por cidadão");

        Assert.Equal(IssueStatus.RESOLVED, report.Status);
        Assert.Equal("Conserto confirmado por cidadão", report.History.Last().Notes);
    }

    [Fact]
    public void So_ocorrencia_resolvida_pode_ser_reaberta()
    {
        var analysis = new AiAnalysis(1, "yolo", AnalysisStatus.COMPLETED, Opened);
        var open = IssueReport.FromDetection(analysis.AddDetection(2, 0.9m, Severity.HIGH), "Buraco", Opened);

        Assert.Throws<DomainException>(() => open.ReopenByCitizen(Sent, "Reaberta"));
    }
}

public class PhotoFormatTests
{
    [Fact]
    public void Reconhece_jpeg_e_png_pela_assinatura_do_arquivo()
    {
        Assert.Equal(PhotoFormat.Jpeg, PhotoFormat.Detect([0xFF, 0xD8, 0xFF, 0xE0, 0x00]));
        Assert.Equal(PhotoFormat.Png, PhotoFormat.Detect([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]));
    }

    [Theory]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46 })]       // PDF
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38 })]       // GIF
    [InlineData(new byte[] { 0xFF, 0xD8 })]                   // curto demais
    public void Recusa_o_que_nao_e_jpeg_nem_png(byte[] content)
    {
        Assert.Null(PhotoFormat.Detect(content));
    }
}
