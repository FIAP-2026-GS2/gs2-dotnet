using SampaTech.Domain.Common;
using SampaTech.Domain.Enums;

namespace SampaTech.Domain.Entities;

public class Camera
{
    public const int CodeMaxLength = 30;
    public const int DescriptionMaxLength = 200;

    public int Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string? Description { get; private set; }
    public CameraStatus Status { get; private set; }
    public DateOnly? InstallationDate { get; private set; }

    // A localização fica no ponto monitorado (MONITORING_POINT), e não na câmera.
    public int MonitoringPointId { get; private set; }
    public MonitoringPoint? MonitoringPoint { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private Camera() { }

    public Camera(
        string code,
        int monitoringPointId,
        string? description = null,
        DateOnly? installationDate = null)
    {
        Update(code, monitoringPointId, description, installationDate);
        Status = CameraStatus.ACTIVE;
    }

    // O Domain só confere o formato do id; se o ponto existe, quem confere é o Application.
    public void Update(string code, int monitoringPointId, string? description, DateOnly? installationDate)
    {
        Code = Guard.RequiredText(code, "código", CodeMaxLength);
        MonitoringPointId = Guard.RequiredId(monitoringPointId, "ponto monitorado");
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
        InstallationDate = installationDate;
    }

    public void ChangeStatus(CameraStatus status) => Status = Guard.DefinedEnum(status, "status");
}
