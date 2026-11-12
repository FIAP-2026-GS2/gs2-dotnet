using SampaTech.Domain.Entities;
using SampaTech.Domain.Enums;

namespace SampaTech.Application.Captures;

// A imagem chega junto com a captura: o serviço de IA já extraiu, borrou e enviou o frame ao Blob.
public record CaptureImageRequest(string FileName, string FilePath, string? FileHash, bool? IsAnonymized);

/// <summary>
/// Cada origem usa campos diferentes:
/// RECORDING_FRAME: cameraId + captureScheduleId; EVENT: cameraId + triggerEventId;
/// CITIZEN: userId + latitude + longitude.
/// captureDate é o momento da gravação (aceita qualquer fuso; é guardado em UTC).
/// </summary>
public record CreateCaptureRequest(
    CaptureSource? Source,
    DateTimeOffset? CaptureDate,
    int? CameraId,
    int? CaptureScheduleId,
    int? TriggerEventId,
    string? UserId,
    decimal? Latitude,
    decimal? Longitude,
    CaptureImageRequest Image);

public record ImageResponse(int Id, string FileName, string FilePath, string? FileHash, DateTime StorageDate)
{
    public static ImageResponse FromEntity(Image image) =>
        new(image.Id, image.FileName, image.FilePath, image.FileHash, image.StorageDate);
}

// O id do usuário (cidadão) não é exposto: a resposta não carrega dados de pessoas.
public record CaptureResponse(
    int Id,
    CaptureSource Source,
    CaptureStatus Status,
    DateTime CaptureDate,
    int? CameraId,
    int? CaptureScheduleId,
    int? TriggerEventId,
    decimal? Latitude,
    decimal? Longitude,
    IReadOnlyList<ImageResponse> Images)
{
    public static CaptureResponse FromEntity(Capture capture, IEnumerable<Image> images) => new(
        capture.Id, capture.Source, capture.Status, capture.CaptureDate, capture.CameraId,
        capture.CaptureScheduleId, capture.TriggerEventId, capture.Latitude, capture.Longitude,
        images.Select(ImageResponse.FromEntity).ToList());
}

public record CaptureQuery(
    int Page = 1,
    int PageSize = 10,
    int? CameraId = null,
    CaptureSource? Source = null,
    CaptureStatus? Status = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);
