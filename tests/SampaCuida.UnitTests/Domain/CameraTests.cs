using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.UnitTests.Domain;

public class CameraTests
{
    private static Camera CreateCamera(
        string code = "CAM-001",
        string description = "Cruzamento principal",
        string address = "Av. Paulista, 1000",
        string neighborhood = "Bela Vista",
        decimal latitude = -23.561414m,
        decimal longitude = -46.655881m) =>
        new(code, description, address, neighborhood, latitude, longitude, new DateOnly(2025, 3, 10));

    [Fact]
    public void Constructor_with_valid_data_creates_active_camera()
    {
        var camera = CreateCamera();

        Assert.Equal("CAM-001", camera.Code);
        Assert.Equal(-23.561414m, camera.Latitude);
        Assert.Equal(new DateOnly(2025, 3, 10), camera.InstalledAt);
        Assert.Equal(CameraStatus.ACTIVE, camera.Status);
    }

    [Fact]
    public void Constructor_removes_spaces_around_text()
    {
        var camera = CreateCamera(code: "  CAM-002  ");

        Assert.Equal("CAM-002", camera.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_blank_code(string code)
    {
        var exception = Assert.Throws<DomainException>(() => CreateCamera(code: code));

        Assert.Contains("código", exception.Message);
    }

    [Fact]
    public void Constructor_rejects_code_longer_than_limit()
    {
        var code = new string('A', Camera.CodeMaxLength + 1);

        Assert.Throws<DomainException>(() => CreateCamera(code: code));
    }

    [Theory]
    [InlineData(-90.000001)]
    [InlineData(90.000001)]
    [InlineData(500)]
    public void Constructor_rejects_latitude_out_of_range(double latitude)
    {
        var exception = Assert.Throws<DomainException>(() => CreateCamera(latitude: (decimal)latitude));

        Assert.Contains("latitude", exception.Message);
    }

    [Theory]
    [InlineData(-180.000001)]
    [InlineData(180.000001)]
    public void Constructor_rejects_longitude_out_of_range(double longitude)
    {
        var exception = Assert.Throws<DomainException>(() => CreateCamera(longitude: (decimal)longitude));

        Assert.Contains("longitude", exception.Message);
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    public void Constructor_accepts_coordinates_on_the_limits(int latitude, int longitude)
    {
        var camera = CreateCamera(latitude: latitude, longitude: longitude);

        Assert.Equal(latitude, camera.Latitude);
        Assert.Equal(longitude, camera.Longitude);
    }
}
