using SampaTech.Application.Common.Paging;

namespace SampaTech.Application.MonitoringPoints;

public interface IMonitoringPointService
{
    Task<PagedResponse<MonitoringPointResponse>> ListAsync(MonitoringPointQuery query, CancellationToken cancellationToken);
    Task<MonitoringPointResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<MonitoringPointResponse> CreateAsync(MonitoringPointRequest request, CancellationToken cancellationToken);
    Task<MonitoringPointResponse> UpdateAsync(int id, MonitoringPointRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
