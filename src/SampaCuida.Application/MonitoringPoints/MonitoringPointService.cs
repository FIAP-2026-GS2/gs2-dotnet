using Microsoft.EntityFrameworkCore;
using SampaCuida.Application.Abstractions;
using SampaCuida.Application.Common.Exceptions;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Application.MonitoringPoints;

public class MonitoringPointService(IAppDbContext db) : IMonitoringPointService
{
    public async Task<PagedResponse<MonitoringPointResponse>> ListAsync(
        MonitoringPointQuery query, CancellationToken cancellationToken)
    {
        var points = db.MonitoringPoints.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Neighborhood))
            points = points.Where(point => point.Neighborhood == query.Neighborhood.Trim());

        if (!string.IsNullOrWhiteSpace(query.City))
            points = points.Where(point => point.City == query.City.Trim());

        return await points
            .OrderBy(point => point.Id)
            .ToPagedAsync(query.Page, query.PageSize, MonitoringPointResponse.FromEntity, cancellationToken);
    }

    public async Task<MonitoringPointResponse> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        MonitoringPointResponse.FromEntity(await FindAsync(id, cancellationToken));

    public async Task<MonitoringPointResponse> CreateAsync(
        MonitoringPointRequest request, CancellationToken cancellationToken)
    {
        var point = new MonitoringPoint(
            request.Street, request.City, request.State, request.Latitude!.Value, request.Longitude!.Value,
            request.StreetNumber, request.Neighborhood, request.Description);

        db.MonitoringPoints.Add(point);
        await db.SaveChangesAsync(cancellationToken);

        return MonitoringPointResponse.FromEntity(point);
    }

    public async Task<MonitoringPointResponse> UpdateAsync(
        int id, MonitoringPointRequest request, CancellationToken cancellationToken)
    {
        var point = await FindAsync(id, cancellationToken);

        point.Update(
            request.Street, request.City, request.State, request.Latitude!.Value, request.Longitude!.Value,
            request.StreetNumber, request.Neighborhood, request.Description);

        await db.SaveChangesAsync(cancellationToken);
        return MonitoringPointResponse.FromEntity(point);
    }

    // Se o ponto ainda tem câmeras, a FK (Restrict) recusa e a API responde 409.
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var point = await FindAsync(id, cancellationToken);

        db.MonitoringPoints.Remove(point);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<MonitoringPoint> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.MonitoringPoints.FirstOrDefaultAsync(point => point.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Ponto monitorado {id} não encontrado.");
}
