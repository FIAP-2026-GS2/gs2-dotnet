using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.UnitTests.Domain;

public class CaptureTests
{
    private static readonly DateTime Noon = new(2026, 11, 10, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FromSchedule_creates_pending_frame_linked_to_camera_and_schedule()
    {
        var capture = Capture.FromSchedule(cameraId: 3, captureScheduleId: 7, Noon);

        Assert.Equal(CaptureSource.RECORDING_FRAME, capture.Source);
        Assert.Equal(CaptureStatus.PENDING, capture.Status);
        Assert.Equal(3, capture.CameraId);
        Assert.Equal(7, capture.CaptureScheduleId);
        Assert.Null(capture.TriggerEventId);
        Assert.Null(capture.UserId);
        Assert.Null(capture.Latitude);
    }

    [Fact]
    public void FromEvent_links_camera_and_trigger_event()
    {
        var capture = Capture.FromEvent(cameraId: 3, triggerEventId: 2, Noon);

        Assert.Equal(CaptureSource.EVENT, capture.Source);
        Assert.Equal(2, capture.TriggerEventId);
        Assert.Null(capture.CaptureScheduleId);
    }

    [Fact]
    public void FromCitizen_keeps_user_and_coordinates_without_camera()
    {
        var capture = Capture.FromCitizen("user-42", -23.55m, -46.63m, Noon);

        Assert.Equal(CaptureSource.CITIZEN, capture.Source);
        Assert.Equal("user-42", capture.UserId);
        Assert.Equal(-23.55m, capture.Latitude);
        Assert.Null(capture.CameraId);
    }

    [Theory]
    [InlineData(0, 7)]
    [InlineData(3, 0)]
    public void FromSchedule_rejects_missing_links(int cameraId, int scheduleId)
    {
        Assert.Throws<DomainException>(() => Capture.FromSchedule(cameraId, scheduleId, Noon));
    }

    [Fact]
    public void FromCitizen_rejects_missing_user_and_invalid_coordinates()
    {
        Assert.Throws<DomainException>(() => Capture.FromCitizen(" ", -23.55m, -46.63m, Noon));
        Assert.Throws<DomainException>(() => Capture.FromCitizen("user-42", 95m, -46.63m, Noon));
    }

    [Fact]
    public void Capture_date_must_be_utc()
    {
        var local = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Local);

        var exception = Assert.Throws<DomainException>(() => Capture.FromSchedule(3, 7, local));

        Assert.Contains("UTC", exception.Message);
    }

    [Fact]
    public void MarkAnalyzed_changes_status_only_once()
    {
        var capture = Capture.FromSchedule(3, 7, Noon);

        capture.MarkAnalyzed();

        Assert.Equal(CaptureStatus.ANALYZED, capture.Status);
        Assert.Throws<DomainException>(() => capture.MarkFailed());
    }

    [Fact]
    public void MarkFailed_changes_status_from_pending()
    {
        var capture = Capture.FromSchedule(3, 7, Noon);

        capture.MarkFailed();

        Assert.Equal(CaptureStatus.FAILED, capture.Status);
    }
}
