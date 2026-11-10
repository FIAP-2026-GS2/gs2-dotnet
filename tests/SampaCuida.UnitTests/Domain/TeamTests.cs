using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.UnitTests.Domain;

public class TeamTests
{
    [Fact]
    public void Constructor_with_valid_data_creates_active_team()
    {
        var team = new Team("Zeladoria Sé - Asfalto", municipalAgencyId: 1, description: "  ");

        Assert.Equal("Zeladoria Sé - Asfalto", team.Name);
        Assert.Equal(1, team.MunicipalAgencyId);
        Assert.Null(team.Description);
        Assert.True(team.IsActive);
    }

    [Fact]
    public void Constructor_rejects_blank_name()
    {
        var exception = Assert.Throws<DomainException>(() => new Team("  ", municipalAgencyId: 1));

        Assert.Contains("nome", exception.Message);
    }

    [Fact]
    public void Constructor_rejects_name_longer_than_limit()
    {
        var name = new string('A', Team.NameMaxLength + 1);

        Assert.Throws<DomainException>(() => new Team(name, municipalAgencyId: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_rejects_missing_agency(int municipalAgencyId)
    {
        var exception = Assert.Throws<DomainException>(() => new Team("Equipe A", municipalAgencyId));

        Assert.Contains("órgão", exception.Message);
    }

    [Fact]
    public void Deactivate_and_activate_change_the_flag_and_can_be_repeated()
    {
        var team = new Team("Equipe A", municipalAgencyId: 1);

        team.Deactivate();
        team.Deactivate();
        Assert.False(team.IsActive);

        team.Activate();
        Assert.True(team.IsActive);
    }
}
