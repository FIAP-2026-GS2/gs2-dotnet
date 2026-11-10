using SampaCuida.Domain.Common;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Domain.Entities;

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
        StartDate = RequireUtc(startDate, "início");
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
        Intensity = Guard.OptionalText(intensity, "intensidade", IntensityMaxLength);
        Status = TriggerEventStatus.ACTIVE;
    }

    // Encerrar duas vezes é erro: a segunda chamada trocaria a data de fim já registrada.
    public void End(DateTime endDate)
    {
        if (!IsActive)
            throw new DomainException("O evento já foi encerrado.");

        RequireUtc(endDate, "fim");

        if (endDate < StartDate)
            throw new DomainException("O fim do evento não pode ser anterior ao início.");

        EndDate = endDate;
        Status = TriggerEventStatus.ENDED;
    }

    // UTC evita ambiguidade: o servidor no Azure roda em UTC, e o horário de
    // São Paulo é só uma forma de exibir.
    private static DateTime RequireUtc(DateTime value, string fieldName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new DomainException($"A data de {fieldName} deve estar em UTC.");

        return value;
    }
}
