using Microsoft.Extensions.DependencyInjection;
using SampaTech.Application;
using SampaTech.Application.Captures;
using SampaTech.Domain.Enums;

namespace SampaTech.UnitTests.Application;

public class CaptureValidatorTests
{
    // Aplica a mesma configuração da API (mensagens em português e chaves em camelCase).
    static CaptureValidatorTests() => new ServiceCollection().AddApplication();

    private static readonly CaptureImageRequest ValidImage = new(
        "cam-001_2026-11-10T15-00.jpg",
        "https://sampatech.blob.core.windows.net/frames/cam-001_2026-11-10T15-00.jpg",
        FileHash: null,
        IsAnonymized: true);

    private static CreateCaptureRequest Frame(CaptureImageRequest? image = null, int? scheduleId = 7) => new(
        CaptureSource.RECORDING_FRAME, DateTimeOffset.Parse("2026-11-10T12:00:00-03:00"),
        CameraId: 3, CaptureScheduleId: scheduleId, TriggerEventId: null,
        UserId: null, Latitude: null, Longitude: null, image ?? ValidImage);

    private static Dictionary<string, string[]> Errors(CreateCaptureRequest request) =>
        new CreateCaptureRequestValidator().Validate(request).ToDictionary()
            .ToDictionary(entry => entry.Key, entry => entry.Value);

    [Fact]
    public void Valid_recording_frame_passes()
    {
        Assert.Empty(Errors(Frame()));
    }

    [Fact]
    public void Recording_frame_requires_schedule()
    {
        Assert.Contains("captureScheduleId", Errors(Frame(scheduleId: null)).Keys);
    }

    [Fact]
    public void Event_capture_requires_trigger_event_and_not_schedule()
    {
        var request = Frame(scheduleId: null) with { Source = CaptureSource.EVENT };

        var errors = Errors(request);

        Assert.Contains("triggerEventId", errors.Keys);
        Assert.DoesNotContain("captureScheduleId", errors.Keys);
    }

    [Fact]
    public void Citizen_capture_requires_user_and_coordinates()
    {
        var request = new CreateCaptureRequest(
            CaptureSource.CITIZEN, DateTimeOffset.UtcNow, null, null, null,
            UserId: null, Latitude: null, Longitude: null, ValidImage);

        var errors = Errors(request);

        Assert.Contains("userId", errors.Keys);
        Assert.Contains("latitude", errors.Keys);
        Assert.Contains("longitude", errors.Keys);
    }

    [Fact]
    public void Image_must_be_anonymized_and_on_https()
    {
        var image = ValidImage with { IsAnonymized = false, FilePath = "http://exemplo.com/frame.jpg" };

        var errors = Errors(Frame(image));

        Assert.Contains("image.isAnonymized", errors.Keys);
        Assert.Contains("image.filePath", errors.Keys);
    }
}
