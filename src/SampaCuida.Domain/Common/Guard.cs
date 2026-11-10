using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Domain.Common;

/// <summary>Validações reutilizadas pelas entidades.</summary>
public static class Guard
{
    public static string RequiredText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"O campo {fieldName} é obrigatório.");

        return CheckLength(value.Trim(), fieldName, maxLength);
    }

    // Texto em branco vira null: "não informado" tem um único jeito de ser guardado.
    public static string? OptionalText(string? value, string fieldName, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : CheckLength(value.Trim(), fieldName, maxLength);

    public static void Coordinates(decimal latitude, decimal longitude)
    {
        if (latitude is < -90 or > 90)
            throw new DomainException("A latitude deve estar entre -90 e 90.");

        if (longitude is < -180 or > 180)
            throw new DomainException("A longitude deve estar entre -180 e 180.");
    }

    private static string CheckLength(string value, string fieldName, int maxLength)
    {
        if (value.Length > maxLength)
            throw new DomainException($"O campo {fieldName} deve ter no máximo {maxLength} caracteres.");

        return value;
    }
}
