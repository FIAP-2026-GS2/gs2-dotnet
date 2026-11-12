using SampaTech.Application.Common.Paging;

namespace SampaTech.Application.Cameras;

public interface ICameraService
{
    Task<PagedResponse<CameraResponse>> ListAsync(CameraQuery query, CancellationToken cancellationToken);
    Task<CameraResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<CameraResponse> CreateAsync(CreateCameraRequest request, CancellationToken cancellationToken);
    Task<CameraResponse> UpdateAsync(int id, UpdateCameraRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
