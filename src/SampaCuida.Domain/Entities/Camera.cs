using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Domain.Entities;

public class Camera
{
    public const int CodeMaxLength = 30;
    public const int DescriptionMaxLength = 200;
    public const int AddressMaxLength = 200;
    public const int NeighborhoodMaxLength = 80;

    public int Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public CameraStatus Status { get; private set; }
    public string Address { get; private set; } = null!;
    public string Neighborhood { get; private set; } = null!;
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public DateOnly InstalledAt { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private Camera() { }

    public Camera(
        string code,
        string description,
        string address,
        string neighborhood,
        decimal latitude,
        decimal longitude,
        DateOnly installedAt)
    {
        Code = RequiredText(code, "código", CodeMaxLength);
        Description = RequiredText(description, "descrição", DescriptionMaxLength);
        Address = RequiredText(address, "endereço", AddressMaxLength);
        Neighborhood = RequiredText(neighborhood, "bairro", NeighborhoodMaxLength);

        if (latitude is < -90 or > 90)
            throw new DomainException("A latitude deve estar entre -90 e 90.");

        if (longitude is < -180 or > 180)
            throw new DomainException("A longitude deve estar entre -180 e 180.");

        Latitude = latitude;
        Longitude = longitude;
        InstalledAt = installedAt;
        Status = CameraStatus.ACTIVE;
    }

    private static string RequiredText(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"O campo {fieldName} é obrigatório.");

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            throw new DomainException($"O campo {fieldName} deve ter no máximo {maxLength} caracteres.");

        return trimmed;
    }
}
