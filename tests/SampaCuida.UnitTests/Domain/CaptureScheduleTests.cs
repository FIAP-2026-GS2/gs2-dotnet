using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.UnitTests.Domain;

public class CaptureScheduleTests
{
    private static readonly DateOnly Start = new(2026, 11, 10);

    [Fact]
    public void Constructor_creates_active_daily_schedule_at_noon()
    {
        var schedule = new CaptureSchedule(cameraId: 1, CaptureSchedule.Noon, Start);

        Assert.Equal(new TimeOnly(12, 0), schedule.ExecutionTime);
        Assert.Null(schedule.Weekday);
        Assert.Null(schedule.EndDate);
        Assert.True(schedule.IsActive);
    }

    [Fact]
    public void Constructor_accepts_midnight_and_weekday()
    {
        var schedule = new CaptureSchedule(1, CaptureSchedule.Midnight, Start, weekday: DayOfWeek.Monday);

        Assert.Equal(new TimeOnly(0, 0), schedule.ExecutionTime);
        Assert.Equal(DayOfWeek.Monday, schedule.Weekday);
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(12, 30)]
    [InlineData(23, 59)]
    public void Constructor_rejects_time_other_than_noon_or_midnight(int hour, int minute)
    {
        var exception = Assert.Throws<DomainException>(() =>
            new CaptureSchedule(1, new TimeOnly(hour, minute), Start));

        Assert.Contains("12:00 ou 00:00", exception.Message);
    }

    [Fact]
    public void Constructor_rejects_end_before_start()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new CaptureSchedule(1, CaptureSchedule.Noon, Start, endDate: Start.AddDays(-1)));

        Assert.Contains("anterior", exception.Message);
    }

    [Fact]
    public void Constructor_accepts_one_day_period()
    {
        var schedule = new CaptureSchedule(1, CaptureSchedule.Noon, Start, endDate: Start);

        Assert.Equal(Start, schedule.EndDate);
    }

    [Fact]
    public void Constructor_rejects_missing_camera()
    {
        Assert.Throws<DomainException>(() => new CaptureSchedule(0, CaptureSchedule.Noon, Start));
    }

    [Fact]
    public void Constructor_rejects_unknown_weekday()
    {
        Assert.Throws<DomainException>(() =>
            new CaptureSchedule(1, CaptureSchedule.Noon, Start, weekday: (DayOfWeek)9));
    }
}
