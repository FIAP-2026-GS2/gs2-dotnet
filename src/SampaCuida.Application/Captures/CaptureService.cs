using Microsoft.EntityFrameworkCore;
using SampaCuida.Application.Abstractions;
using SampaCuida.Application.Common.Exceptions;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Application.Captures;

public interface ICaptureService
{
    Task<PagedResponse<CaptureResponse>> ListAsync(CaptureQuery query, CancellationToken cancellationToken);
    Task<CaptureResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<CaptureResponse> CreateAsync(CreateCaptureRequest request, CancellationToken cancellationToken);
}

public class CaptureService(IAppDbContext db, TimeProvider timeProvider) : ICaptureService
{
    public async Task<PagedResponse<CaptureResponse>> ListAsync(CaptureQuery query, CancellationToken cancellationToken)
    {
        var captures = db.Captures.AsNoTracking();

        if (query.CameraId is not null)
            captures = captures.Where(capture => capture.CameraId == query.CameraId);

        if (query.Source is not null)
            captures = captures.Where(capture => capture.Source == query.Source);

        if (query.Status is not null)
            captures = captures.Where(capture => capture.Status == query.Status);

        if (query.From is not null)
            captures = captures.Where(capture => capture.CaptureDate >= query.From.Value.UtcDateTime);

        if (query.To is not null)
            captures = captures.Where(capture => capture.CaptureDate <= query.To.Value.UtcDateTime);

        var totalItems = await captures.CountAsync(cancellationToken);
        var page = await captures
            .OrderByDescending(capture => capture.CaptureDate)
            .ThenByDescending(capture => capture.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        // Busca as imagens da página inteira de uma vez (uma consulta, e não uma por captura).
        var captureIds = page.Select(capture => capture.Id).ToList();
        var images = await db.Images.AsNoTracking()
            .Where(image => captureIds.Contains(image.CaptureId))
            .ToListAsync(cancellationToken);

        var items = page
            .Select(capture => CaptureResponse.FromEntity(capture, images.Where(image => image.CaptureId == capture.Id)))
            .ToList();

        return new PagedResponse<CaptureResponse>(items, query.Page, query.PageSize, totalItems);
    }

    public async Task<CaptureResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var capture = await db.Captures.AsNoTracking()
            .FirstOrDefaultAsync(capture => capture.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Captura {id} não encontrada.");

        var images = await db.Images.AsNoTracking()
            .Where(image => image.CaptureId == id)
            .ToListAsync(cancellationToken);

        return CaptureResponse.FromEntity(capture, images);
    }

    // Captura e imagem são gravadas juntas: um único SaveChanges, numa transação só.
    // Frame repetido (mesma câmera e mesmo instante) é recusado pelo índice único: 409.
    public async Task<CaptureResponse> CreateAsync(CreateCaptureRequest request, CancellationToken cancellationToken)
    {
        var captureDate = request.CaptureDate!.Value.UtcDateTime;

        var capture = request.Source switch
        {
            CaptureSource.RECORDING_FRAME => await FromScheduleAsync(request, captureDate, cancellationToken),
            CaptureSource.EVENT => await FromEventAsync(request, captureDate, cancellationToken),
            _ => Capture.FromCitizen(request.UserId!, request.Latitude!.Value, request.Longitude!.Value, captureDate)
        };

        var image = new Image(
            capture, request.Image.FileName, request.Image.FilePath, request.Image.IsAnonymized == true,
            timeProvider.GetUtcNow().UtcDateTime, request.Image.FileHash);

        db.Captures.Add(capture);
        db.Images.Add(image);
        await db.SaveChangesAsync(cancellationToken);

        return CaptureResponse.FromEntity(capture, [image]);
    }

    private async Task<Capture> FromScheduleAsync(
        CreateCaptureRequest request, DateTime captureDate, CancellationToken cancellationToken)
    {
        var cameraId = request.CameraId!.Value;
        await EnsureCameraIsActiveAsync(cameraId, cancellationToken);

        var schedule = await db.CaptureSchedules.AsNoTracking().FirstOrDefaultAsync(
            schedule => schedule.Id == request.CaptureScheduleId && schedule.CameraId == cameraId, cancellationToken)
            ?? throw new DomainException($"A agenda {request.CaptureScheduleId} não existe na câmera {cameraId}.");

        if (!schedule.IsActive)
            throw new DomainException($"A agenda {schedule.Id} está desativada.");

        return Capture.FromSchedule(cameraId, schedule.Id, captureDate);
    }

    private async Task<Capture> FromEventAsync(
        CreateCaptureRequest request, DateTime captureDate, CancellationToken cancellationToken)
    {
        var cameraId = request.CameraId!.Value;
        await EnsureCameraIsActiveAsync(cameraId, cancellationToken);

        var triggerEvent = await db.TriggerEvents.AsNoTracking().FirstOrDefaultAsync(
            triggerEvent => triggerEvent.Id == request.TriggerEventId, cancellationToken)
            ?? throw new DomainException($"O alerta {request.TriggerEventId} não existe.");

        if (!triggerEvent.IsActive)
            throw new DomainException($"O alerta {triggerEvent.Id} já foi encerrado.");

        return Capture.FromEvent(cameraId, triggerEvent.Id, captureDate);
    }

    // Câmera em manutenção ou desativada não gera capturas.
    private async Task EnsureCameraIsActiveAsync(int cameraId, CancellationToken cancellationToken)
    {
        var status = await db.Cameras.AsNoTracking()
            .Where(camera => camera.Id == cameraId)
            .Select(camera => (CameraStatus?)camera.Status)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainException($"A câmera {cameraId} não existe.");

        if (status != CameraStatus.ACTIVE)
            throw new DomainException($"A câmera {cameraId} não está ativa (status: {status}).");
    }
}
