using SampaTech.Domain.Enums;

namespace SampaTech.Domain.Common;

/// <summary>
/// Prioridade de 0 a 100 da fila do gestor, calculada na hora (não é guardada).
/// Severidade pesa mais (até 60); ver o mesmo problema várias vezes confirma que é real (até 25);
/// e o que está aberto há dias sobe na fila, para não ser esquecido (até 15).
/// </summary>
public static class IssuePriority
{
    public const int MaxRecurrenceBonus = 25;
    public const int PointsPerExtraDetection = 5;
    public const int MaxAgeBonus = 15;

    public static int Calculate(Severity? severity, int detectionCount, DateTime openingDate, DateTime nowUtc)
    {
        // Sem severidade estimada, conta como LOW.
        var severityPoints = severity switch
        {
            Severity.CRITICAL => 60,
            Severity.HIGH => 45,
            Severity.MEDIUM => 30,
            _ => 15
        };

        // A primeira detecção é a que abriu a ocorrência; só as seguintes somam.
        var recurrencePoints = Math.Min(
            Math.Max(detectionCount - 1, 0) * PointsPerExtraDetection, MaxRecurrenceBonus);

        // 1 ponto por dia completo aberta.
        var daysOpen = (int)Math.Floor((nowUtc - openingDate).TotalDays);
        var agePoints = Math.Clamp(daysOpen, 0, MaxAgeBonus);

        return severityPoints + recurrencePoints + agePoints;
    }
}
