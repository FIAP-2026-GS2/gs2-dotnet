using SampaTech.Domain.Exceptions;

namespace SampaTech.Domain.Common;

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

    // Sigla da UF (ex.: "sp" vira "SP"); nome por extenso é recusado.
    public static string StateCode(string? value)
    {
        // Limite folgado só para chegar à mensagem certa ("sigla com 2 letras").
        var state = RequiredText(value, "estado", 50).ToUpperInvariant();

        if (state.Length != 2 || !state.All(char.IsAsciiLetterUpper))
            throw new DomainException("O estado deve ser a sigla da UF com 2 letras (ex.: SP).");

        return state;
    }

    // Instantes ficam em UTC: o servidor no Azure roda em UTC, e o horário de
    // São Paulo é só uma forma de exibir.
    public static DateTime Utc(DateTime value, string fieldName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new DomainException($"A data de {fieldName} deve estar em UTC.");

        return value;
    }

    public static int RequiredId(int id, string fieldName)
    {
        if (id <= 0)
            throw new DomainException($"O campo {fieldName} é obrigatório.");

        return id;
    }

    // Recusa números que não correspondem a nenhum valor do enum (ex.: (IssueCategory)99).
    public static TEnum DefinedEnum<TEnum>(TEnum value, string fieldName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new DomainException($"O valor informado para {fieldName} é inválido.");

        return value;
    }

    private static string CheckLength(string value, string fieldName, int maxLength)
    {
        if (value.Length > maxLength)
            throw new DomainException($"O campo {fieldName} deve ter no máximo {maxLength} caracteres.");

        return value;
    }
}
