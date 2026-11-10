using SampaCuida.Domain.Common;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Domain.Entities;

/// <summary>Local físico monitorado (ex.: um cruzamento), que pode ter várias câmeras.</summary>
public class MonitoringPoint
{
    public const int StreetMaxLength = 150;
    public const int StreetNumberMaxLength = 10;
    public const int NeighborhoodMaxLength = 80;
    public const int CityMaxLength = 100;
    public const int StateLength = 2;
    public const int DescriptionMaxLength = 200;

    public int Id { get; private set; }
    public string Street { get; private set; } = null!;
    public string? StreetNumber { get; private set; }
    public string? Neighborhood { get; private set; }
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public string? Description { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private MonitoringPoint() { }

    public MonitoringPoint(
        string street,
        string city,
        string state,
        decimal latitude,
        decimal longitude,
        string? streetNumber = null,
        string? neighborhood = null,
        string? description = null)
    {
        Guard.Coordinates(latitude, longitude);

        Street = Guard.RequiredText(street, "rua", StreetMaxLength);
        City = Guard.RequiredText(city, "cidade", CityMaxLength);
        State = NormalizeState(state);
        Latitude = latitude;
        Longitude = longitude;
        StreetNumber = Guard.OptionalText(streetNumber, "número", StreetNumberMaxLength);
        Neighborhood = Guard.OptionalText(neighborhood, "bairro", NeighborhoodMaxLength);
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
    }

    // Sigla da UF (ex.: "sp" vira "SP"); nome por extenso é recusado.
    private static string NormalizeState(string state)
    {
        var value = Guard.RequiredText(state, "estado", StateLength).ToUpperInvariant();

        if (value.Length != StateLength || !value.All(char.IsAsciiLetterUpper))
            throw new DomainException("O estado deve ser a sigla da UF com 2 letras (ex.: SP).");

        return value;
    }
}
