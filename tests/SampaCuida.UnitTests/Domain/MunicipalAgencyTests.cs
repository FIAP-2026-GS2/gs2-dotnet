using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.UnitTests.Domain;

public class MunicipalAgencyTests
{
    private static MunicipalAgency CreateAgency(
        string name = "Subprefeitura Sé",
        string city = "São Paulo",
        string state = "SP",
        string? description = null,
        string? subprefecture = "Sé") =>
        new(name, city, state, description, subprefecture);

    [Fact]
    public void Constructor_with_valid_data_creates_active_agency()
    {
        var agency = CreateAgency();

        Assert.Equal("Subprefeitura Sé", agency.Name);
        Assert.Equal("SP", agency.State);
        Assert.Equal("Sé", agency.Subprefecture);
        Assert.True(agency.IsActive);
    }

    [Theory]
    [InlineData("", "São Paulo", "nome")]
    [InlineData("Subprefeitura Sé", " ", "cidade")]
    public void Constructor_rejects_blank_required_text(string name, string city, string field)
    {
        var exception = Assert.Throws<DomainException>(() => CreateAgency(name: name, city: city));

        Assert.Contains(field, exception.Message);
    }

    [Fact]
    public void Constructor_rejects_state_that_is_not_two_letters()
    {
        var exception = Assert.Throws<DomainException>(() => CreateAgency(state: "São Paulo"));

        Assert.Contains("sigla", exception.Message);
    }

    [Fact]
    public void Deactivate_and_activate_change_the_flag_and_can_be_repeated()
    {
        var agency = CreateAgency();

        agency.Deactivate();
        agency.Deactivate();
        Assert.False(agency.IsActive);

        agency.Activate();
        agency.Activate();
        Assert.True(agency.IsActive);
    }
}
