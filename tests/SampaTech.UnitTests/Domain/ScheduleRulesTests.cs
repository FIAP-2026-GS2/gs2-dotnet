using SampaTech.Domain.Entities;
using SampaTech.Domain.Enums;

namespace SampaTech.UnitTests.Domain;

// Regras de "o que extrair agora", testadas com horários fixos (sem relógio de verdade).
public class ScheduleRulesTests
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(30);
    private static readonly DateOnly Friday = new(2026, 11, 13);

    private static CaptureSchedule Noon(DateOnly? endDate = null, DayOfWeek? weekday = null) =>
        new(cameraId: 7, CaptureSchedule.Noon, new DateOnly(2026, 11, 10), endDate, weekday);

    [Theory]
    [InlineData(12, 0, true)]
    [InlineData(12, 29, true)]
    [InlineData(12, 30, false)]
    [InlineData(11, 59, false)]
    [InlineData(18, 0, false)]
    public void Noon_schedule_is_due_only_inside_the_window(int hour, int minute, bool expected)
    {
        Assert.Equal(expected, Noon().IsDueAt(Friday, new TimeOnly(hour, minute), Tolerance));
    }

    [Fact]
    public void Midnight_schedule_is_due_just_after_midnight()
    {
        var midnight = new CaptureSchedule(7, CaptureSchedule.Midnight, new DateOnly(2026, 11, 10));

        Assert.True(midnight.IsDueAt(Friday, new TimeOnly(0, 10), Tolerance));
        Assert.False(midnight.IsDueAt(Friday, new TimeOnly(23, 50), Tolerance));
    }

    [Fact]
    public void Schedule_respects_weekday()
    {
        var mondaysOnly = Noon(weekday: DayOfWeek.Monday);

        Assert.False(mondaysOnly.IsDueAt(Friday, new TimeOnly(12, 5), Tolerance));
        Assert.True(mondaysOnly.IsDueAt(new DateOnly(2026, 11, 16), new TimeOnly(12, 5), Tolerance));
    }

    [Fact]
    public void Schedule_respects_its_period()
    {
        var untilThursday = Noon(endDate: new DateOnly(2026, 11, 12));

        Assert.False(untilThursday.IsDueAt(Friday, new TimeOnly(12, 5), Tolerance));
        Assert.False(Noon().IsDueAt(new DateOnly(2026, 11, 9), new TimeOnly(12, 5), Tolerance));
    }

    [Fact]
    public void Inactive_schedule_is_never_due()
    {
        var schedule = Noon();
        schedule.Deactivate();

        Assert.False(schedule.IsDueAt(Friday, new TimeOnly(12, 5), Tolerance));
    }

    [Fact]
    public void Analyses_come_from_the_time()
    {
        var midnight = new CaptureSchedule(7, CaptureSchedule.Midnight, new DateOnly(2026, 11, 10));

        Assert.Equal([AnalysisType.POTHOLE_CROSSWALK, AnalysisType.STREETLIGHT], Noon().Analyses);
        Assert.Equal([AnalysisType.STREETLIGHT], midnight.Analyses);
    }

    // Alerta começou às 18:00 UTC (15:00 em SP); intervalo de 3 h.
    private static readonly DateTime RainStart = new(2026, 11, 13, 18, 0, 0, DateTimeKind.Utc);

    private static TriggerEvent Rain() =>
        new(TriggerEventType.HEAVY_RAIN, "Defesa Civil - SMS", RainStart);

    [Theory]
    [InlineData(0, 0, 0)]     // no próprio início
    [InlineData(0, 20, 0)]    // 20 min depois: ainda o slot do início
    [InlineData(3, 10, 3)]    // 3h10 depois: slot de 3 h
    [InlineData(6, 0, 6)]     // 6 h depois: slot de 6 h
    public void Rain_slot_repeats_every_interval(int hours, int minutes, int expectedSlotHours)
    {
        var now = RainStart.AddHours(hours).AddMinutes(minutes);

        var slot = Rain().CurrentSlot(now, TimeSpan.FromHours(3), Tolerance);

        Assert.Equal(RainStart.AddHours(expectedSlotHours), slot);
    }

    [Theory]
    [InlineData(0, 40)]
    [InlineData(2, 0)]
    [InlineData(-1, 0)]
    public void Rain_has_no_slot_outside_the_windows(int hours, int minutes)
    {
        var now = RainStart.AddHours(hours).AddMinutes(minutes);

        Assert.Null(Rain().CurrentSlot(now, TimeSpan.FromHours(3), Tolerance));
    }

    [Fact]
    public void Ended_rain_has_no_slot()
    {
        var rain = Rain();
        rain.End(RainStart.AddHours(1));

        Assert.Null(rain.CurrentSlot(RainStart.AddHours(3), TimeSpan.FromHours(3), Tolerance));
    }
}
