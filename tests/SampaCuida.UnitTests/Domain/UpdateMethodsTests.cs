using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.UnitTests.Domain;

// Os métodos Update reaproveitam as validações do construtor.
public class UpdateMethodsTests
{
    [Fact]
    public void MunicipalAgency_update_changes_data_and_validates_state()
    {
        var agency = new MunicipalAgency("Subprefeitura Sé", "São Paulo", "SP");

        agency.Update("Subprefeitura Pinheiros", "São Paulo", "sp", "Zona oeste", "Pinheiros");

        Assert.Equal("Subprefeitura Pinheiros", agency.Name);
        Assert.Equal("SP", agency.State);
        Assert.Throws<DomainException>(() => agency.Update("X", "São Paulo", "São Paulo", null, null));
    }

    [Fact]
    public void Team_update_changes_agency_and_rejects_missing_one()
    {
        var team = new Team("Equipe A", municipalAgencyId: 1);

        team.Update("Equipe A - Asfalto", municipalAgencyId: 2, description: "Turno da manhã");

        Assert.Equal(2, team.MunicipalAgencyId);
        Assert.Throws<DomainException>(() => team.Update("Equipe A", 0, null));
    }

    [Fact]
    public void IssueType_update_changes_category()
    {
        var issueType = new IssueType("Poste apagado", IssueCategory.BURACO);

        issueType.Update("Poste apagado", IssueCategory.POSTE_APAGADO, null);

        Assert.Equal(IssueCategory.POSTE_APAGADO, issueType.Category);
    }

    [Fact]
    public void CaptureSchedule_update_keeps_camera_and_validates_time()
    {
        var schedule = new CaptureSchedule(cameraId: 3, CaptureSchedule.Noon, new DateOnly(2026, 11, 10));

        schedule.Update(CaptureSchedule.Midnight, new DateOnly(2026, 11, 11), null, DayOfWeek.Friday);

        Assert.Equal(3, schedule.CameraId);
        Assert.Equal(CaptureSchedule.Midnight, schedule.ExecutionTime);
        Assert.Equal(DayOfWeek.Friday, schedule.Weekday);
        Assert.Throws<DomainException>(() =>
            schedule.Update(new TimeOnly(9, 0), new DateOnly(2026, 11, 11), null, null));
    }

    [Fact]
    public void TriggerEvent_update_details_only_while_active()
    {
        var start = new DateTime(2026, 11, 10, 18, 0, 0, DateTimeKind.Utc);
        var triggerEvent = new TriggerEvent(TriggerEventType.HEAVY_RAIN, "Defesa Civil - SMS", start);

        triggerEvent.UpdateDetails("Defesa Civil - App", "Alerta para a zona sul", "Muito forte");
        Assert.Equal("Muito forte", triggerEvent.Intensity);

        triggerEvent.End(start.AddHours(2));
        var exception = Assert.Throws<DomainException>(() =>
            triggerEvent.UpdateDetails("Defesa Civil", null, null));

        Assert.Contains("encerrado", exception.Message);
    }
}
