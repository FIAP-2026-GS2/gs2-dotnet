using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.UnitTests.Domain;

public class TriggerEventTests
{
    private static readonly DateTime Start = new(2026, 11, 10, 18, 0, 0, DateTimeKind.Utc);

    private static TriggerEvent CreateEvent(DateTime? startDate = null) =>
        new(TriggerEventType.HEAVY_RAIN, "Defesa Civil - SMS", startDate ?? Start, intensity: "Forte");

    [Fact]
    public void Constructor_creates_active_event()
    {
        var triggerEvent = CreateEvent();

        Assert.Equal(TriggerEventStatus.ACTIVE, triggerEvent.Status);
        Assert.True(triggerEvent.IsActive);
        Assert.Equal(Start, triggerEvent.StartDate);
        Assert.Null(triggerEvent.EndDate);
        Assert.Equal("Forte", triggerEvent.Intensity);
    }

    [Fact]
    public void Constructor_rejects_blank_alert_source()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new TriggerEvent(TriggerEventType.HEAVY_RAIN, " ", Start));

        Assert.Contains("fonte do alerta", exception.Message);
    }

    [Fact]
    public void Constructor_rejects_start_date_that_is_not_utc()
    {
        var localDate = new DateTime(2026, 11, 10, 15, 0, 0, DateTimeKind.Local);

        var exception = Assert.Throws<DomainException>(() => CreateEvent(localDate));

        Assert.Contains("UTC", exception.Message);
    }

    [Fact]
    public void End_closes_the_event()
    {
        var triggerEvent = CreateEvent();
        var end = Start.AddHours(3);

        triggerEvent.End(end);

        Assert.Equal(TriggerEventStatus.ENDED, triggerEvent.Status);
        Assert.False(triggerEvent.IsActive);
        Assert.Equal(end, triggerEvent.EndDate);
    }

    [Fact]
    public void End_rejects_date_before_start()
    {
        var triggerEvent = CreateEvent();

        var exception = Assert.Throws<DomainException>(() => triggerEvent.End(Start.AddMinutes(-1)));

        Assert.Contains("anterior", exception.Message);
        Assert.True(triggerEvent.IsActive);
    }

    [Fact]
    public void End_rejects_event_already_ended()
    {
        var triggerEvent = CreateEvent();
        var firstEnd = Start.AddHours(1);
        triggerEvent.End(firstEnd);

        var exception = Assert.Throws<DomainException>(() => triggerEvent.End(Start.AddHours(5)));

        Assert.Contains("já foi encerrado", exception.Message);
        Assert.Equal(firstEnd, triggerEvent.EndDate);
    }
}
