using SampaTech.Domain.Common;
using SampaTech.Domain.Enums;
using SampaTech.Domain.Exceptions;

namespace SampaTech.Domain.Entities;

/// <summary>
/// Frame extraído de uma câmera ou foto enviada por um cidadão.
/// Cada origem exige ligações diferentes, por isso a criação é feita por métodos
/// de fábrica (FromSchedule, FromEvent, FromCitizen): uma combinação inválida
/// nem consegue ser escrita.
/// </summary>
public class Capture
{
    public const int UserIdMaxLength = 100;

    public int Id { get; private set; }
    public CaptureSource Source { get; private set; }

    // Momento da gravação (no vídeo ou na foto), em UTC.
    public DateTime CaptureDate { get; private set; }
    public CaptureStatus Status { get; private set; }

    // Só para fotos de cidadão; nos frames, a localização vem do ponto da câmera.
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }

    public int? CameraId { get; private set; }
    public Camera? Camera { get; private set; }
    public int? CaptureScheduleId { get; private set; }
    public CaptureSchedule? CaptureSchedule { get; private set; }
    public int? TriggerEventId { get; private set; }
    public TriggerEvent? TriggerEvent { get; private set; }

    // Usuário é da API Java (outro banco): guardamos só o id do token, sem FK.
    public string? UserId { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private Capture() { }

    private Capture(CaptureSource source, DateTime captureDate)
    {
        Source = source;
        CaptureDate = Guard.Utc(captureDate, "captura");
        Status = CaptureStatus.PENDING;
    }

    public static Capture FromSchedule(int cameraId, int captureScheduleId, DateTime captureDate) =>
        new(CaptureSource.RECORDING_FRAME, captureDate)
        {
            CameraId = Guard.RequiredId(cameraId, "câmera"),
            CaptureScheduleId = Guard.RequiredId(captureScheduleId, "agenda")
        };

    public static Capture FromEvent(int cameraId, int triggerEventId, DateTime captureDate) =>
        new(CaptureSource.EVENT, captureDate)
        {
            CameraId = Guard.RequiredId(cameraId, "câmera"),
            TriggerEventId = Guard.RequiredId(triggerEventId, "alerta")
        };

    public static Capture FromCitizen(string userId, decimal latitude, decimal longitude, DateTime captureDate)
    {
        Guard.Coordinates(latitude, longitude);

        return new(CaptureSource.CITIZEN, captureDate)
        {
            UserId = Guard.RequiredText(userId, "usuário", UserIdMaxLength),
            Latitude = latitude,
            Longitude = longitude
        };
    }

    public void MarkAnalyzed() => ChangeStatusFromPending(CaptureStatus.ANALYZED);

    public void MarkFailed() => ChangeStatusFromPending(CaptureStatus.FAILED);

    private void ChangeStatusFromPending(CaptureStatus newStatus)
    {
        if (Status != CaptureStatus.PENDING)
            throw new DomainException($"A captura já está com status {Status}.");

        Status = newStatus;
    }
}
