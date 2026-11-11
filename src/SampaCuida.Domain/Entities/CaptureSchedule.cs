using SampaCuida.Domain.Common;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Domain.Entities;

/// <summary>
/// Regra de relógio de uma câmera ("todo dia ao meio-dia"). O horário é de São Paulo,
/// e não UTC: é como um despertador, programado na hora local.
/// </summary>
public class CaptureSchedule
{
    // O tipo de análise sai do horário: ao meio-dia, buraco, faixa e poste "de dia";
    // à meia-noite, o poste "de noite". Por isso só esses dois horários existem.
    public static readonly TimeOnly Noon = new(12, 0);
    public static readonly TimeOnly Midnight = new(0, 0);

    public int Id { get; private set; }
    public int CameraId { get; private set; }
    public Camera? Camera { get; private set; }
    public DayOfWeek? Weekday { get; private set; }
    public TimeOnly ExecutionTime { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsActive { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private CaptureSchedule() { }

    public CaptureSchedule(
        int cameraId,
        TimeOnly executionTime,
        DateOnly startDate,
        DateOnly? endDate = null,
        DayOfWeek? weekday = null)
    {
        CameraId = Guard.RequiredId(cameraId, "câmera");
        Update(executionTime, startDate, endDate, weekday);
        IsActive = true;
    }

    // A câmera não muda: para outra câmera, cria-se outra agenda.
    public void Update(TimeOnly executionTime, DateOnly startDate, DateOnly? endDate, DayOfWeek? weekday)
    {
        if (executionTime != Noon && executionTime != Midnight)
            throw new DomainException("O horário da agenda deve ser 12:00 ou 00:00.");

        if (endDate < startDate)
            throw new DomainException("O fim da agenda não pode ser anterior ao início.");

        ExecutionTime = executionTime;
        StartDate = startDate;
        EndDate = endDate;
        // Sem dia da semana, a agenda vale para todos os dias.
        Weekday = weekday is null ? null : Guard.DefinedEnum(weekday.Value, "dia da semana");
    }

    // Desativar em vez de apagar preserva o histórico. Repetir a chamada não muda nada.
    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
