using SampaTech.Domain.Common;

namespace SampaTech.Domain.Entities;

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
        Update(street, city, state, latitude, longitude, streetNumber, neighborhood, description);
    }

    // O construtor e a atualização passam pelas mesmas validações: um único lugar com as regras.
    public void Update(
        string street,
        string city,
        string state,
        decimal latitude,
        decimal longitude,
        string? streetNumber,
        string? neighborhood,
        string? description)
    {
        Guard.Coordinates(latitude, longitude);

        Street = Guard.RequiredText(street, "rua", StreetMaxLength);
        City = Guard.RequiredText(city, "cidade", CityMaxLength);
        State = Guard.StateCode(state);
        Latitude = latitude;
        Longitude = longitude;
        StreetNumber = Guard.OptionalText(streetNumber, "número", StreetNumberMaxLength);
        Neighborhood = Guard.OptionalText(neighborhood, "bairro", NeighborhoodMaxLength);
        Description = Guard.OptionalText(description, "descrição", DescriptionMaxLength);
    }
}
