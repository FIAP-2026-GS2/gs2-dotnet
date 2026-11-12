using SampaTech.Domain.Entities;
using SampaTech.Domain.Exceptions;

namespace SampaTech.UnitTests.Domain;

public class MonitoringPointTests
{
    private static MonitoringPoint CreatePoint(
        string street = "Av. Paulista",
        string city = "São Paulo",
        string state = "SP",
        decimal latitude = -23.561414m,
        decimal longitude = -46.655881m,
        string? streetNumber = "1000",
        string? neighborhood = "Bela Vista",
        string? description = "Cruzamento com a R. Augusta") =>
        new(street, city, state, latitude, longitude, streetNumber, neighborhood, description);

    [Fact]
    public void Constructor_with_valid_data_creates_point()
    {
        var point = CreatePoint();

        Assert.Equal("Av. Paulista", point.Street);
        Assert.Equal("SP", point.State);
        Assert.Equal(-23.561414m, point.Latitude);
        Assert.Equal("Bela Vista", point.Neighborhood);
    }

    [Theory]
    [InlineData("", "São Paulo", "rua")]
    [InlineData("Av. Paulista", "   ", "cidade")]
    public void Constructor_rejects_blank_required_text(string street, string city, string field)
    {
        var exception = Assert.Throws<DomainException>(() => CreatePoint(street: street, city: city));

        Assert.Contains(field, exception.Message);
    }

    [Fact]
    public void Constructor_rejects_street_longer_than_limit()
    {
        var street = new string('A', MonitoringPoint.StreetMaxLength + 1);

        Assert.Throws<DomainException>(() => CreatePoint(street: street));
    }

    [Fact]
    public void Constructor_converts_state_to_uppercase()
    {
        var point = CreatePoint(state: " sp ");

        Assert.Equal("SP", point.State);
    }

    [Theory]
    [InlineData("São Paulo")]
    [InlineData("S")]
    [InlineData("S1")]
    public void Constructor_rejects_state_that_is_not_two_letters(string state)
    {
        Assert.Throws<DomainException>(() => CreatePoint(state: state));
    }

    [Theory]
    [InlineData(-90.000001, 0)]
    [InlineData(90.000001, 0)]
    [InlineData(0, -180.000001)]
    [InlineData(0, 180.000001)]
    public void Constructor_rejects_coordinates_out_of_range(double latitude, double longitude)
    {
        Assert.Throws<DomainException>(() =>
            CreatePoint(latitude: (decimal)latitude, longitude: (decimal)longitude));
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    public void Constructor_accepts_coordinates_on_the_limits(int latitude, int longitude)
    {
        var point = CreatePoint(latitude: latitude, longitude: longitude);

        Assert.Equal(latitude, point.Latitude);
        Assert.Equal(longitude, point.Longitude);
    }

    [Fact]
    public void Update_changes_data_and_keeps_validations()
    {
        var point = CreatePoint();

        point.Update("Rua Augusta", "São Paulo", "sp", -23.55m, -46.65m, "500", null, null);

        Assert.Equal("Rua Augusta", point.Street);
        Assert.Equal("SP", point.State);
        Assert.Null(point.Neighborhood);
        Assert.Throws<DomainException>(() =>
            point.Update("Rua Augusta", "São Paulo", "SP", 91m, -46.65m, null, null, null));
    }

    [Fact]
    public void Constructor_turns_blank_optional_text_into_null()
    {
        var point = CreatePoint(streetNumber: "  ", neighborhood: "", description: null);

        Assert.Null(point.StreetNumber);
        Assert.Null(point.Neighborhood);
        Assert.Null(point.Description);
    }
}
