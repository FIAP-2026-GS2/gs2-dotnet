using System.Net;
using System.Net.Http.Json;
using SampaTech.Application.Analyses;
using SampaTech.Application.Cameras;
using SampaTech.Application.CaptureSchedules;
using SampaTech.Application.Captures;
using SampaTech.Application.IssueTypes;
using SampaTech.Application.MonitoringPoints;
using SampaTech.Domain.Enums;

namespace SampaTech.IntegrationTests.Infrastructure;

/// <summary>Monta os dados pelos próprios endpoints, como um cliente real faria.</summary>
public static class Seed
{
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json.Options))!;

    public static async Task<T> PostCreatedAsync<T>(this HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body, Json.Options);
        Assert.True(response.StatusCode == HttpStatusCode.Created,
            $"POST {url} respondeu {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return await response.ReadAsync<T>();
    }

    public static Task<MonitoringPointResponse> MonitoringPointAsync(this HttpClient client, string streetNumber = "1500") =>
        client.PostCreatedAsync<MonitoringPointResponse>("/api/monitoring-points", new MonitoringPointRequest(
            "Rua Augusta", "São Paulo", "SP", -23.5558m, -46.6623m, streetNumber, "Consolação", null));

    public static Task<CameraResponse> CameraAsync(this HttpClient client, int monitoringPointId, string code = "CAM-001") =>
        client.PostCreatedAsync<CameraResponse>("/api/cameras",
            new CreateCameraRequest(code, monitoringPointId, null, null));

    public static Task<CaptureScheduleResponse> ScheduleAsync(this HttpClient client, int cameraId) =>
        client.PostCreatedAsync<CaptureScheduleResponse>($"/api/cameras/{cameraId}/schedules",
            new CaptureScheduleRequest(new TimeOnly(12, 0), new DateOnly(2026, 11, 1), null, null));

    public static Task<IssueTypeResponse> IssueTypeAsync(this HttpClient client, string name = "Buraco na via") =>
        client.PostCreatedAsync<IssueTypeResponse>("/api/issue-types",
            new IssueTypeRequest(name, IssueCategory.BURACO, null));

    /// <summary>Câmera pronta para receber capturas: ponto, câmera, agenda e um tipo de problema.</summary>
    public static async Task<(CameraResponse Camera, CaptureScheduleResponse Schedule, IssueTypeResponse IssueType)>
        CameraWithScheduleAsync(this HttpClient admin, string code = "CAM-001")
    {
        var point = await admin.MonitoringPointAsync();
        var camera = await admin.CameraAsync(point.Id, code);
        var schedule = await admin.ScheduleAsync(camera.Id);
        var issueType = await admin.IssueTypeAsync();
        return (camera, schedule, issueType);
    }

    public static CreateCaptureRequest Capture(int cameraId, int scheduleId, DateTimeOffset? date = null, bool anonymized = true) =>
        new(CaptureSource.RECORDING_FRAME, date ?? new DateTimeOffset(2026, 11, 10, 15, 0, 0, TimeSpan.Zero),
            cameraId, scheduleId, null, null, null, null,
            new CaptureImageRequest("frame.jpg", "https://sampatech.blob.core.windows.net/frames/frame.jpg", null, anonymized));

    public static CreateAnalysisRequest Analysis(int imageId, int issueTypeId, decimal confidence = 0.9m) =>
        new(imageId, "yolo-zeladoria", "1.0", AnalysisStatus.COMPLETED, new DateTimeOffset(2026, 11, 10, 15, 1, 0, TimeSpan.Zero),
            850, [new DetectionRequest(issueTypeId, confidence, Severity.HIGH, null, null, null, null)]);

    /// <summary>POST com Idempotency-Key (o serviço de IA sempre manda uma).</summary>
    public static Task<HttpResponseMessage> PostIdempotentAsync(this HttpClient client, string url, object body, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body, options: Json.Options) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }
}
