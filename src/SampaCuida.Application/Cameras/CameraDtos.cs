using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;

namespace SampaCuida.Application.Cameras;

public record CreateCameraRequest(
    string Code,
    int MonitoringPointId,
    string? Description,
    DateOnly? InstallationDate);

// Na atualização, o status também pode mudar (ex.: MAINTENANCE).
public record UpdateCameraRequest(
    string Code,
    int MonitoringPointId,
    string? Description,
    DateOnly? InstallationDate,
    CameraStatus? Status);

public record CameraResponse(
    int Id,
    string Code,
    string? Description,
    CameraStatus Status,
    DateOnly? InstallationDate,
    int MonitoringPointId)
{
    public static CameraResponse FromEntity(Camera camera) => new(
        camera.Id, camera.Code, camera.Description, camera.Status, camera.InstallationDate,
        camera.MonitoringPointId);
}

public record CameraQuery(
    int Page = 1,
    int PageSize = 10,
    int? MonitoringPointId = null,
    CameraStatus? Status = null);
