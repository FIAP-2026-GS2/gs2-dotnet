using SampaTech.Domain.Entities;
using SampaTech.Domain.Enums;
using SampaTech.Domain.Exceptions;

namespace SampaTech.UnitTests.Domain;

public class IssueTypeTests
{
    [Theory]
    [InlineData(IssueCategory.BURACO)]
    [InlineData(IssueCategory.FAIXA_APAGADA)]
    [InlineData(IssueCategory.POSTE_APAGADO)]
    [InlineData(IssueCategory.VIA_ALAGADA)]
    public void Constructor_accepts_every_category(IssueCategory category)
    {
        var issueType = new IssueType("Tipo de teste", category);

        Assert.Equal(category, issueType.Category);
        Assert.True(issueType.IsActive);
    }

    [Fact]
    public void Constructor_rejects_unknown_category()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new IssueType("Buraco no asfalto", (IssueCategory)99));

        Assert.Contains("categoria", exception.Message);
    }

    [Fact]
    public void Constructor_rejects_blank_name()
    {
        Assert.Throws<DomainException>(() => new IssueType("", IssueCategory.BURACO));
    }

    [Fact]
    public void Constructor_keeps_description_and_trims_name()
    {
        var issueType = new IssueType("  Buraco no asfalto ", IssueCategory.BURACO, "Cavidade na pista");

        Assert.Equal("Buraco no asfalto", issueType.Name);
        Assert.Equal("Cavidade na pista", issueType.Description);
    }

    [Fact]
    public void Deactivate_and_activate_change_the_flag_and_can_be_repeated()
    {
        var issueType = new IssueType("Buraco no asfalto", IssueCategory.BURACO);

        issueType.Deactivate();
        issueType.Deactivate();
        Assert.False(issueType.IsActive);

        issueType.Activate();
        Assert.True(issueType.IsActive);
    }
}
