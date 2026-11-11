using SampaCuida.Domain.Enums;

namespace SampaCuida.Domain.Common;

/// <summary>
/// Onde está um problema na rua: nome da rua, altura (número do imóvel; em SP a numeração
/// é métrica, então 1500 fica a cerca de 1.500 m do início da rua) e lado.
/// </summary>
public sealed record IssueLocation(string? Street, int? Level, StreetSide? Side)
{
    // Diferença máxima de altura para considerar o mesmo problema (≈ 30 metros).
    public const int MaxLevelDistance = 30;

    /// <summary>
    /// Regra da deduplicação: mesma rua, lados compatíveis e até 30 de diferença na altura.
    /// Lado não informado é compatível com qualquer lado (a IA nem sempre sabe dizer).
    /// </summary>
    public bool IsSameSpot(IssueLocation other)
    {
        if (string.IsNullOrWhiteSpace(Street) || string.IsNullOrWhiteSpace(other.Street))
            return false;

        if (!string.Equals(Street.Trim(), other.Street.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (Level is null || other.Level is null)
            return false;

        var sidesAreCompatible = Side is null || other.Side is null || Side == other.Side;

        return sidesAreCompatible && Math.Abs(Level.Value - other.Level.Value) <= MaxLevelDistance;
    }

    // "1500", "1500-A" ou "1500 fundos" viram 1500; texto sem número vira null.
    public static int? LevelFromStreetNumber(string? streetNumber)
    {
        var digits = new string((streetNumber ?? "").Trim().TakeWhile(char.IsAsciiDigit).ToArray());
        return int.TryParse(digits, out var level) ? level : null;
    }
}
