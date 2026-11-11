using SampaCuida.Domain.Common;
using SampaCuida.Domain.Enums;

namespace SampaCuida.UnitTests.Domain;

public class IssuePriorityTests
{
    private static readonly DateTime Opened = new(2026, 11, 10, 15, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(Severity.LOW, 15)]
    [InlineData(Severity.MEDIUM, 30)]
    [InlineData(Severity.HIGH, 45)]
    [InlineData(Severity.CRITICAL, 60)]
    [InlineData(null, 15)]
    public void Severity_gives_the_base_points(Severity? severity, int expected)
    {
        Assert.Equal(expected, IssuePriority.Calculate(severity, detectionCount: 1, Opened, Opened));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 5)]
    [InlineData(4, 15)]
    [InlineData(6, 25)]
    [InlineData(20, 25)]
    public void Each_extra_detection_adds_5_up_to_25(int detectionCount, int expectedBonus)
    {
        var priority = IssuePriority.Calculate(Severity.LOW, detectionCount, Opened, Opened);

        Assert.Equal(15 + expectedBonus, priority);
    }

    [Theory]
    [InlineData(0.5, 0)]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(40, 15)]
    public void Each_full_day_open_adds_1_up_to_15(double days, int expectedBonus)
    {
        var priority = IssuePriority.Calculate(Severity.LOW, 1, Opened, Opened.AddDays(days));

        Assert.Equal(15 + expectedBonus, priority);
    }

    [Fact]
    public void Maximum_priority_is_100()
    {
        Assert.Equal(100, IssuePriority.Calculate(Severity.CRITICAL, 50, Opened, Opened.AddDays(90)));
    }

    [Fact]
    public void Low_issue_seen_many_times_and_old_beats_a_new_medium()
    {
        var oldLow = IssuePriority.Calculate(Severity.LOW, 6, Opened, Opened.AddDays(15));
        var newMedium = IssuePriority.Calculate(Severity.MEDIUM, 1, Opened, Opened);

        Assert.True(oldLow > newMedium);
    }
}
