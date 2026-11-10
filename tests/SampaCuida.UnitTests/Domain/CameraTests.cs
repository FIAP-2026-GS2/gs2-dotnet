using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.UnitTests.Domain;

public class CameraTests
{
    private static Camera CreateCamera(
        string code = "CAM-001",
        int monitoringPointId = 1,
        string? description = "Câmera voltada para a faixa",
        DateOnly? installationDate = null) =>
        new(code, monitoringPointId, description, installationDate);

    [Fact]
    public void Constructor_with_valid_data_creates_active_camera()
    {
        var camera = CreateCamera(installationDate: new DateOnly(2025, 3, 10));

        Assert.Equal("CAM-001", camera.Code);
        Assert.Equal(1, camera.MonitoringPointId);
        Assert.Equal(new DateOnly(2025, 3, 10), camera.InstallationDate);
        Assert.Equal(CameraStatus.ACTIVE, camera.Status);
    }

    [Fact]
    public void Constructor_removes_spaces_around_code()
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
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_rejects_missing_monitoring_point(int monitoringPointId)
    {
        var exception = Assert.Throws<DomainException>(() =>
            CreateCamera(monitoringPointId: monitoringPointId));

        Assert.Contains("ponto monitorado", exception.Message);
    }

    [Fact]
    public void Constructor_accepts_camera_without_optional_fields()
    {
        var camera = CreateCamera(description: "   ", installationDate: null);

        Assert.Null(camera.Description);
        Assert.Null(camera.InstallationDate);
    }
}
