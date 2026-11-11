using SampaCuida.Domain.Entities;

namespace SampaCuida.Application.MonitoringPoints;

// Coordenadas como decimal? para "não informado" virar erro de validação, e não 0.
public record MonitoringPointRequest(
    string Street,
    string City,
    string State,
    decimal? Latitude,
    decimal? Longitude,
    string? StreetNumber,
    string? Neighborhood,
    string? Description);

public record MonitoringPointResponse(
    int Id,
    string Street,
    string? StreetNumber,
    string? Neighborhood,
    string City,
    string State,
    decimal Latitude,
    decimal Longitude,
    string? Description)
{
    public static MonitoringPointResponse FromEntity(MonitoringPoint point) => new(
        point.Id, point.Street, point.StreetNumber, point.Neighborhood, point.City, point.State,
        point.Latitude, point.Longitude, point.Description);
}

public record MonitoringPointQuery(int Page = 1, int PageSize = 10, string? Neighborhood = null, string? City = null);
