using SampaCuida.Domain.Common;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Domain.Entities;

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
        // O Domain só confere o formato; se o ponto existe, quem confere é o Application.
        if (monitoringPointId <= 0)
            throw new DomainException("O ponto monitorado é obrigatório.");

        Code = Guard.RequiredText(code, "código", CodeMaxLength);
        MonitoringPointId = monitoringPointId;
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
        InstallationDate = installationDate;
        Status = CameraStatus.ACTIVE;
    }
}
