using SampaTech.Domain.Common;
using SampaTech.Domain.Enums;
using SampaTech.Domain.Exceptions;

namespace SampaTech.Domain.Entities;

/// <summary>
/// Alerta externo (ex.: chuva forte avisada pela Defesa Civil). Enquanto estiver ativo,
/// a agenda pede capturas de alagamento. Datas sempre em UTC.
/// </summary>
public class TriggerEvent
{
    public const int DescriptionMaxLength = 200;
    public const int IntensityMaxLength = 50;
    public const int AlertSourceMaxLength = 100;

    public int Id { get; private set; }
    public TriggerEventType EventType { get; private set; }
    public string? Description { get; private set; }
    public string? Intensity { get; private set; }
    public string AlertSource { get; private set; } = null!;
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public TriggerEventStatus Status { get; private set; }

    public bool IsActive => Status == TriggerEventStatus.ACTIVE;

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private TriggerEvent() { }

    public TriggerEvent(
        TriggerEventType eventType,
        string alertSource,
        DateTime startDate,
        string? description = null,
        string? intensity = null)
    {
        EventType = Guard.DefinedEnum(eventType, "tipo de evento");
        AlertSource = Guard.RequiredText(alertSource, "fonte do alerta", AlertSourceMaxLength);
        StartDate = Guard.Utc(startDate, "início");
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
        Intensity = Guard.OptionalText(intensity, "intensidade", IntensityMaxLength);
        Status = TriggerEventStatus.ACTIVE;
    }

    /// <summary>
    /// Horário do frame de chuva que vale agora, ou null se não há frame devido.
    /// Os horários começam no início do alerta e se repetem a cada intervalo
    /// (ex.: alerta às 15:00 e intervalo de 3 h: 15:00, 18:00, 21:00...).
    /// Às 18:10, com tolerância de 30 min, o horário devido é 18:00; às 18:40, nenhum.
    /// </summary>
    public DateTime? CurrentSlot(DateTime nowUtc, TimeSpan interval, TimeSpan tolerance)
    {
        if (interval <= TimeSpan.Zero || tolerance <= TimeSpan.Zero)
            throw new DomainException("O intervalo e a tolerância do alerta devem ser positivos.");

        if (!IsActive || nowUtc < StartDate)
            return null;

        var completedIntervals = (nowUtc - StartDate).Ticks / interval.Ticks;
        var slot = StartDate.AddTicks(completedIntervals * interval.Ticks);

        return nowUtc - slot < tolerance ? slot : null;
    }

    // Tipo e início não mudam depois de criado: só os dados descritivos.
    public void UpdateDetails(string alertSource, string? description, string? intensity)
    {
        if (!IsActive)
            throw new DomainException("Um evento encerrado não pode ser alterado.");

        AlertSource = Guard.RequiredText(alertSource, "fonte do alerta", AlertSourceMaxLength);
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
        Intensity = Guard.OptionalText(intensity, "intensidade", IntensityMaxLength);
    }

    // Encerrar duas vezes é erro: a segunda chamada trocaria a data de fim já registrada.
    public void End(DateTime endDate)
    {
        if (!IsActive)
            throw new DomainException("O evento já foi encerrado.");

        Guard.Utc(endDate, "fim");

        if (endDate < StartDate)
            throw new DomainException("O fim do evento não pode ser anterior ao início.");

        EndDate = endDate;
        Status = TriggerEventStatus.ENDED;
    }
}
