using SampaTech.Domain.Common;
using SampaTech.Domain.Enums;

namespace SampaTech.UnitTests.Domain;

public class IssueLocationTests
{
    private static readonly IssueLocation Paulista1500Odd = new("Av. Paulista", 1500, StreetSide.ODD);

    [Theory]
    [InlineData(1500)]
    [InlineData(1525)]
    [InlineData(1530)]
    [InlineData(1470)]
    public void Same_street_and_side_within_30_is_the_same_spot(int level)
    {
        Assert.True(Paulista1500Odd.IsSameSpot(new IssueLocation("Av. Paulista", level, StreetSide.ODD)));
    }

    [Theory]
    [InlineData(1531)]
    [InlineData(1469)]
    [InlineData(2000)]
    public void More_than_30_away_is_another_spot(int level)
    {
        Assert.False(Paulista1500Odd.IsSameSpot(new IssueLocation("Av. Paulista", level, StreetSide.ODD)));
    }

    [Fact]
    public void Street_name_ignores_case_and_spaces()
    {
        Assert.True(Paulista1500Odd.IsSameSpot(new IssueLocation("  av. paulista ", 1510, StreetSide.ODD)));
    }

    [Fact]
    public void Another_street_is_another_spot()
    {
        Assert.False(Paulista1500Odd.IsSameSpot(new IssueLocation("Rua Augusta", 1500, StreetSide.ODD)));
    }

    [Fact]
    public void Opposite_sides_are_different_spots()
    {
        Assert.False(Paulista1500Odd.IsSameSpot(new IssueLocation("Av. Paulista", 1500, StreetSide.EVEN)));
    }

    [Fact]
    public void Unknown_side_is_compatible_with_any_side()
    {
        Assert.True(Paulista1500Odd.IsSameSpot(new IssueLocation("Av. Paulista", 1510, Side: null)));
    }

    [Fact]
    public void Without_street_or_level_it_is_never_the_same_spot()
    {
        Assert.False(Paulista1500Odd.IsSameSpot(new IssueLocation(null, 1500, StreetSide.ODD)));
        Assert.False(Paulista1500Odd.IsSameSpot(new IssueLocation("Av. Paulista", null, StreetSide.ODD)));
    }

    [Theory]
    [InlineData("1500", 1500)]
    [InlineData(" 1500-A ", 1500)]
    [InlineData("s/n", null)]
    [InlineData(null, null)]
    public void Level_is_read_from_the_street_number(string? streetNumber, int? expected)
    {
        Assert.Equal(expected, IssueLocation.LevelFromStreetNumber(streetNumber));
    }
}
