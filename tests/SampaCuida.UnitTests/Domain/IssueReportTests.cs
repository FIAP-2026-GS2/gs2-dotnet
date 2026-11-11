using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.UnitTests.Domain;

public class IssueReportTests
{
    private const int PotholeTypeId = 2;
    private static readonly DateTime Opened = new(2026, 11, 10, 15, 5, 0, DateTimeKind.Utc);

    private static AiDetection CreateDetection(int issueTypeId = PotholeTypeId, Severity? severity = Severity.MEDIUM)
    {
        var analysis = new AiAnalysis(1, "yolov8-pothole", AnalysisStatus.COMPLETED, Opened);
        return analysis.AddDetection(issueTypeId, 0.9m, severity, description: "Buraco na faixa da direita");
    }

    private static IssueReport CreateReport(Severity? severity = Severity.MEDIUM) =>
        IssueReport.FromDetection(CreateDetection(severity: severity), "Buraco na Av. Paulista", Opened,
            level: 1500, side: StreetSide.ODD);

    // Leva a ocorrência até IN_PROGRESS, passando pela atribuição.
    private static IssueReport CreateReportInProgress()
    {
        var report = CreateReport();
        report.AssignTo(teamId: 4, Opened.AddHours(1));
        report.ChangeStatus(IssueStatus.IN_PROGRESS, Opened.AddHours(2));
        return report;
    }

    [Fact]
    public void FromDetection_opens_report_linked_to_detection_with_history()
    {
        var detection = CreateDetection();

        var report = IssueReport.FromDetection(detection, "Buraco na Av. Paulista", Opened, 1500, StreetSide.ODD);

        Assert.Equal(IssueStatus.OPEN, report.Status);
        Assert.Equal(IssueReportSource.AI_DETECTION, report.Source);
        Assert.Equal(PotholeTypeId, report.IssueTypeId);
        Assert.Equal(Severity.MEDIUM, report.Severity);
        Assert.Equal("Buraco na faixa da direita", report.Description);
        Assert.Same(report, detection.IssueReport);

        var opening = Assert.Single(report.History);
        Assert.Null(opening.PreviousStatus);
        Assert.Equal(IssueStatus.OPEN, opening.NewStatus);
    }

    [Fact]
    public void FromCitizen_requires_user()
    {
        Assert.Throws<DomainException>(() =>
            IssueReport.FromCitizen(PotholeTypeId, " ", "Poste apagado", Opened));
    }

    [Fact]
    public void Constructor_rejects_negative_level()
    {
        Assert.Throws<DomainException>(() =>
            IssueReport.FromCitizen(PotholeTypeId, "user-1", "Buraco", Opened, level: -1));
    }

    [Fact]
    public void RegisterDetection_links_new_detection_and_raises_severity()
    {
        var report = CreateReport(Severity.MEDIUM);
        var newDetection = CreateDetection(severity: Severity.CRITICAL);

        report.RegisterDetection(newDetection);

        Assert.Same(report, newDetection.IssueReport);
        Assert.Equal(Severity.CRITICAL, report.Severity);
    }

    [Fact]
    public void RaiseSeverity_never_lowers_the_severity()
    {
        var report = CreateReport(Severity.HIGH);

        report.RaiseSeverity(Severity.LOW);
        report.RaiseSeverity(null);

        Assert.Equal(Severity.HIGH, report.Severity);
    }

    [Fact]
    public void RegisterDetection_rejects_detection_of_another_issue_type()
    {
        var report = CreateReport();

        Assert.Throws<DomainException>(() => report.RegisterDetection(CreateDetection(issueTypeId: 9)));
    }

    [Fact]
    public void RegisterDetection_rejects_detection_already_linked()
    {
        var detection = CreateDetection();
        IssueReport.FromDetection(detection, "Primeira", Opened);
        var otherReport = CreateReport();

        Assert.Throws<DomainException>(() => otherReport.RegisterDetection(detection));
    }

    [Fact]
    public void AssignTo_creates_open_assignment_and_changes_status()
    {
        var report = CreateReport();

        var assignment = report.AssignTo(teamId: 4, Opened.AddHours(1), userId: "gestor-1");

        Assert.Equal(IssueStatus.ASSIGNED, report.Status);
        Assert.True(assignment.IsOpen);
        Assert.Equal(4, assignment.TeamId);
        Assert.Equal(IssueStatus.ASSIGNED, report.History.Last().NewStatus);
        Assert.Equal("gestor-1", report.History.Last().UserId);
    }

    [Fact]
    public void AssignTo_twice_is_refused()
    {
        var report = CreateReport();
        report.AssignTo(4, Opened.AddHours(1));

        Assert.Throws<DomainException>(() => report.AssignTo(5, Opened.AddHours(2)));
        Assert.Single(report.Assignments);
    }

    [Fact]
    public void ChangeStatus_to_assigned_directly_is_refused()
    {
        var report = CreateReport();

        Assert.Throws<DomainException>(() => report.ChangeStatus(IssueStatus.ASSIGNED, Opened.AddHours(1)));
    }

    [Fact]
    public void Full_flow_resolves_report_closes_assignment_and_records_every_change()
    {
        var report = CreateReportInProgress();
        var resolvedAt = Opened.AddHours(5);

        report.ChangeStatus(IssueStatus.RESOLVED, resolvedAt, "Buraco tapado.", "gestor-1");

        Assert.Equal(IssueStatus.RESOLVED, report.Status);
        Assert.Equal(resolvedAt, report.ResolutionDate);
        Assert.False(report.IsOpen);
        Assert.Equal(resolvedAt, Assert.Single(report.Assignments).ClosingDate);
        Assert.Equal(
            [IssueStatus.OPEN, IssueStatus.ASSIGNED, IssueStatus.IN_PROGRESS, IssueStatus.RESOLVED],
            report.History.Select(history => history.NewStatus));
    }

    [Fact]
    public void Resolved_report_cannot_go_back_to_open()
    {
        var report = CreateReportInProgress();
        report.ChangeStatus(IssueStatus.RESOLVED, Opened.AddHours(5));

        var exception = Assert.Throws<DomainException>(() =>
            report.ChangeStatus(IssueStatus.OPEN, Opened.AddHours(6)));

        Assert.Contains("RESOLVED para OPEN", exception.Message);
    }

    [Theory]
    [InlineData(IssueStatus.IN_PROGRESS)]
    [InlineData(IssueStatus.RESOLVED)]
    public void Open_report_cannot_skip_the_assignment(IssueStatus newStatus)
    {
        var report = CreateReport();

        Assert.Throws<DomainException>(() => report.ChangeStatus(newStatus, Opened.AddHours(1)));
    }

    [Fact]
    public void Rejecting_assigned_report_closes_the_assignment_without_resolution_date()
    {
        var report = CreateReport();
        report.AssignTo(4, Opened.AddHours(1));

        report.ChangeStatus(IssueStatus.REJECTED, Opened.AddHours(2), "Falso positivo da IA.");

        Assert.Equal(IssueStatus.REJECTED, report.Status);
        Assert.Null(report.ResolutionDate);
        Assert.False(Assert.Single(report.Assignments).IsOpen);
    }

    [Fact]
    public void Closed_report_does_not_accept_new_detections()
    {
        var report = CreateReport();
        report.ChangeStatus(IssueStatus.REJECTED, Opened.AddHours(1), "Falso positivo: sombra de árvore.");

        Assert.Throws<DomainException>(() => report.RegisterDetection(CreateDetection()));
    }

    [Fact]
    public void Rejecting_without_reason_is_refused()
    {
        var report = CreateReport();

        var exception = Assert.Throws<DomainException>(() =>
            report.ChangeStatus(IssueStatus.REJECTED, Opened.AddHours(1), notes: "  "));

        Assert.Contains("motivo", exception.Message);
        Assert.Equal(IssueStatus.OPEN, report.Status);
    }

    [Fact]
    public void Reassign_closes_current_assignment_and_opens_another()
    {
        var report = CreateReport();
        var first = report.AssignTo(teamId: 4, Opened.AddHours(1));

        var second = report.Reassign(teamId: 5, Opened.AddHours(2), userId: "gestor-1");

        Assert.Equal(IssueStatus.ASSIGNED, report.Status);
        Assert.False(first.IsOpen);
        Assert.True(second.IsOpen);
        Assert.Equal(5, second.TeamId);
        Assert.Single(report.Assignments, assignment => assignment.IsOpen);
        Assert.Equal("Equipe trocada.", report.History.Last().Notes);
    }

    [Fact]
    public void Reassign_to_the_same_team_is_refused()
    {
        var report = CreateReport();
        report.AssignTo(4, Opened.AddHours(1));

        Assert.Throws<DomainException>(() => report.Reassign(4, Opened.AddHours(2)));
    }

    [Fact]
    public void Reassign_requires_status_assigned()
    {
        Assert.Throws<DomainException>(() => CreateReport().Reassign(4, Opened.AddHours(1)));
    }
}
