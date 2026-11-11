using Microsoft.EntityFrameworkCore;
using SampaCuida.Application.Abstractions;
using SampaCuida.Application.Common.Exceptions;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Application.Cameras;

public class CameraService(IAppDbContext db) : ICameraService
{
    public async Task<PagedResponse<CameraResponse>> ListAsync(CameraQuery query, CancellationToken cancellationToken)
    {
        var cameras = db.Cameras.AsNoTracking();

        if (query.MonitoringPointId is not null)
            cameras = cameras.Where(camera => camera.MonitoringPointId == query.MonitoringPointId);

        if (query.Status is not null)
            cameras = cameras.Where(camera => camera.Status == query.Status);

        return await cameras
            .OrderBy(camera => camera.Id)
            .ToPagedAsync(query.Page, query.PageSize, CameraResponse.FromEntity, cancellationToken);
    }

    public async Task<CameraResponse> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        CameraResponse.FromEntity(await FindAsync(id, cancellationToken));

    // Código repetido é barrado pelo índice único (409), sem consulta prévia:
    // só o banco garante isso mesmo com duas requisições ao mesmo tempo.
    public async Task<CameraResponse> CreateAsync(CreateCameraRequest request, CancellationToken cancellationToken)
    {
        await EnsureMonitoringPointExistsAsync(request.MonitoringPointId, cancellationToken);

        var camera = new Camera(request.Code, request.MonitoringPointId, request.Description, request.InstallationDate);

        db.Cameras.Add(camera);
        await db.SaveChangesAsync(cancellationToken);

        return CameraResponse.FromEntity(camera);
    }

    public async Task<CameraResponse> UpdateAsync(int id, UpdateCameraRequest request, CancellationToken cancellationToken)
    {
        var camera = await FindAsync(id, cancellationToken);
        await EnsureMonitoringPointExistsAsync(request.MonitoringPointId, cancellationToken);

        camera.Update(request.Code, request.MonitoringPointId, request.Description, request.InstallationDate);
        camera.ChangeStatus(request.Status!.Value);

        await db.SaveChangesAsync(cancellationToken);
        return CameraResponse.FromEntity(camera);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var camera = await FindAsync(id, cancellationToken);

        db.Cameras.Remove(camera);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Camera> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Cameras.FirstOrDefaultAsync(camera => camera.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Câmera {id} não encontrada.");

    // Ponto inexistente no corpo do pedido é dado inválido (400), e não 404:
    // o 404 fica para o recurso da própria rota.
    private async Task EnsureMonitoringPointExistsAsync(int monitoringPointId, CancellationToken cancellationToken)
    {
        if (!await db.MonitoringPoints.AnyAsync(point => point.Id == monitoringPointId, cancellationToken))
            throw new DomainException($"O ponto monitorado {monitoringPointId} não existe.");
    }
}
