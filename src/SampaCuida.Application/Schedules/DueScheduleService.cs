using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SampaCuida.Application.Abstractions;
using SampaCuida.Domain.Enums;

namespace SampaCuida.Application.Schedules;

/// <summary>Configuração "Schedule" do appsettings.</summary>
public class ScheduleOptions
{
    public const string SectionName = "Schedule";

    // Janela depois do horário em que o frame ainda é pedido (ex.: 12:00 até 12:29).
    public int ToleranceMinutes { get; set; } = 30;

    // Durante alerta de chuva, um frame de cada câmera a cada X horas.
    public int FloodingIntervalHours { get; set; } = 3;

    // Fuso das regras de relógio (12:00 e 00:00 são de São Paulo).
    public string TimeZone { get; set; } = "America/Sao_Paulo";
}

public enum DueReason
{
    FIXED_TIME,    // agenda de horário fixo (12:00 ou 00:00)
    TRIGGER_EVENT  // alerta de chuva ativo
}

// "at" é opcional: sem ele, vale o momento atual. Com ele, dá para simular qualquer horário.
public record DueScheduleQuery(DateTimeOffset? At = null);

/// <summary>
/// Um frame a extrair. slotAt é o instante exato do frame, em UTC: o serviço de IA usa
/// o mesmo valor como captureDate, e o índice único (câmera + instante) impede duplicidade.
/// </summary>
public record DueCaptureResponse(
    int CameraId,
    string CameraCode,
    DateTime SlotAt,
    DueReason Reason,
    int? CaptureScheduleId,
    int? TriggerEventId,
    IReadOnlyList<AnalysisType> Analyses);

public interface IDueScheduleService
{
    Task<IReadOnlyList<DueCaptureResponse>> ListDueAsync(DueScheduleQuery query, CancellationToken cancellationToken);
}

public class DueScheduleService(
    IAppDbContext db, TimeProvider timeProvider, IOptions<ScheduleOptions> options) : IDueScheduleService
{
    private readonly ScheduleOptions _options = options.Value;

    public async Task<IReadOnlyList<DueCaptureResponse>> ListDueAsync(
        DueScheduleQuery query, CancellationToken cancellationToken)
    {
        var nowUtc = (query.At ?? timeProvider.GetUtcNow()).UtcDateTime;
        var tolerance = TimeSpan.FromMinutes(_options.ToleranceMinutes);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);

        var candidates = new List<DueCaptureResponse>();
        candidates.AddRange(await FixedTimeAsync(nowUtc, tolerance, timeZone, cancellationToken));
        candidates.AddRange(await RainAsync(nowUtc, tolerance, cancellationToken));

        return await RemoveAlreadyCapturedAsync(candidates, cancellationToken);
    }

    // Agendas de horário fixo: a regra de relógio é de São Paulo, então convertemos o "agora".
    private async Task<IEnumerable<DueCaptureResponse>> FixedTimeAsync(
        DateTime nowUtc, TimeSpan tolerance, TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);
        var localDate = DateOnly.FromDateTime(local);
        var localTime = TimeOnly.FromDateTime(local);

        var schedules = await db.CaptureSchedules.AsNoTracking()
            .Include(schedule => schedule.Camera)
            .Where(schedule => schedule.IsActive && schedule.Camera!.Status == CameraStatus.ACTIVE)
            .ToListAsync(cancellationToken);

        return schedules
            .Where(schedule => schedule.IsDueAt(localDate, localTime, tolerance))
            .Select(schedule => new DueCaptureResponse(
                schedule.CameraId,
                schedule.Camera!.Code,
                // 12:00 do dia em São Paulo, convertido para UTC.
                TimeZoneInfo.ConvertTimeToUtc(localDate.ToDateTime(schedule.ExecutionTime), timeZone),
                DueReason.FIXED_TIME,
                schedule.Id,
                null,
                schedule.Analyses));
    }

    // Alerta de chuva ativo: um frame de todas as câmeras ativas a cada intervalo (cidade toda no MVP).
    private async Task<IEnumerable<DueCaptureResponse>> RainAsync(
        DateTime nowUtc, TimeSpan tolerance, CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromHours(_options.FloodingIntervalHours);

        var activeEvents = await db.TriggerEvents.AsNoTracking()
            .Where(triggerEvent => triggerEvent.Status == TriggerEventStatus.ACTIVE
                                   && triggerEvent.EventType == TriggerEventType.HEAVY_RAIN)
            .ToListAsync(cancellationToken);

        var slots = activeEvents
            .Select(triggerEvent => (Event: triggerEvent, Slot: triggerEvent.CurrentSlot(nowUtc, interval, tolerance)))
            .Where(item => item.Slot is not null)
            .ToList();

        if (slots.Count == 0)
            return [];

        var cameras = await db.Cameras.AsNoTracking()
            .Where(camera => camera.Status == CameraStatus.ACTIVE)
            .Select(camera => new { camera.Id, camera.Code })
            .ToListAsync(cancellationToken);

        return slots.SelectMany(item => cameras.Select(camera => new DueCaptureResponse(
            camera.Id, camera.Code, item.Slot!.Value, DueReason.TRIGGER_EVENT, null, item.Event.Id,
            [AnalysisType.FLOODING])));
    }

    // Tira o que já foi capturado (mesma câmera e mesmo instante), para não pedir de novo.
    private async Task<IReadOnlyList<DueCaptureResponse>> RemoveAlreadyCapturedAsync(
        List<DueCaptureResponse> candidates, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
            return [];

        var cameraIds = candidates.Select(candidate => candidate.CameraId).Distinct().ToList();
        var slotTimes = candidates.Select(candidate => candidate.SlotAt).Distinct().ToList();

        var captured = (await db.Captures.AsNoTracking()
                .Where(capture => capture.CameraId != null
                                  && cameraIds.Contains(capture.CameraId.Value)
                                  && slotTimes.Contains(capture.CaptureDate))
                .Select(capture => new { CameraId = capture.CameraId!.Value, capture.CaptureDate })
                .ToListAsync(cancellationToken))
            .Select(capture => (capture.CameraId, capture.CaptureDate))
            .ToHashSet();

        return candidates
            .Where(candidate => !captured.Contains((candidate.CameraId, candidate.SlotAt)))
            // Dois alertas no mesmo instante não pedem o mesmo frame duas vezes.
            .DistinctBy(candidate => (candidate.CameraId, candidate.SlotAt))
            .OrderBy(candidate => candidate.SlotAt)
            .ThenBy(candidate => candidate.CameraId)
            .ToList();
    }
}
