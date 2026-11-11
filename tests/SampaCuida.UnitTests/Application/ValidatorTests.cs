using SampaCuida.Application;
using SampaCuida.Application.Cameras;
using SampaCuida.Application.CaptureSchedules;
using SampaCuida.Application.MonitoringPoints;
using SampaCuida.Application.TriggerEvents;
using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace SampaCuida.UnitTests.Application;

public class ValidatorTests
{
    // Aplica a mesma configuração da API (mensagens em português e chaves em camelCase).
    static ValidatorTests() => new ServiceCollection().AddApplication();

    private static MonitoringPointRequest ValidPoint() =>
        new("Av. Paulista", "São Paulo", "SP", -23.56m, -46.65m, "1000", "Bela Vista", null);

    [Fact]
    public void Valid_monitoring_point_request_passes()
    {
        var result = new MonitoringPointRequestValidator().Validate(ValidPoint());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Invalid_monitoring_point_request_returns_every_error_at_once()
    {
        var request = ValidPoint() with { Street = "", State = "São Paulo", Latitude = null, Longitude = 200m };

        var errors = new MonitoringPointRequestValidator().Validate(request).ToDictionary();

        Assert.Equal(4, errors.Count);
        Assert.Contains("street", errors.Keys);
        Assert.Contains("state", errors.Keys);
        Assert.Contains("latitude", errors.Keys);
        Assert.Contains("longitude", errors.Keys);
    }

    [Fact]
    public void Create_camera_requires_code_and_monitoring_point()
    {
        var errors = new CreateCameraRequestValidator()
            .Validate(new CreateCameraRequest("", 0, null, null))
            .ToDictionary();

        Assert.Contains("code", errors.Keys);
        Assert.Contains("monitoringPointId", errors.Keys);
    }

    [Fact]
    public void Update_camera_requires_status()
    {
        var errors = new UpdateCameraRequestValidator()
            .Validate(new UpdateCameraRequest("CAM-1", 1, null, null, Status: null))
            .ToDictionary();

        Assert.Contains("status", errors.Keys);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public void Camera_query_rejects_invalid_page(int page, int pageSize)
    {
        var result = new CameraQueryValidator().Validate(new CameraQuery(page, pageSize));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(12, 0, true)]
    [InlineData(0, 0, true)]
    [InlineData(8, 30, false)]
    public void Capture_schedule_accepts_only_noon_or_midnight(int hour, int minute, bool expected)
    {
        var request = new CaptureScheduleRequest(new TimeOnly(hour, minute), new DateOnly(2026, 11, 10), null, null);

        Assert.Equal(expected, new CaptureScheduleRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Capture_schedule_rejects_end_before_start()
    {
        var request = new CaptureScheduleRequest(
            CaptureSchedule.Noon, new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 9), null);

        Assert.False(new CaptureScheduleRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Trigger_event_requires_type_and_alert_source()
    {
        var errors = new CreateTriggerEventRequestValidator()
            .Validate(new CreateTriggerEventRequest(null, " ", null, null, null))
            .ToDictionary();

        Assert.Contains("eventType", errors.Keys);
        Assert.Contains("alertSource", errors.Keys);
    }

    [Fact]
    public void Camera_query_accepts_defaults_and_filters()
    {
        var result = new CameraQueryValidator().Validate(new CameraQuery(Status: CameraStatus.ACTIVE));

        Assert.True(result.IsValid);
    }
}
