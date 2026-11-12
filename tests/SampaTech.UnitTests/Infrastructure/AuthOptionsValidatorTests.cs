using SampaTech.Application.Auth;
using SampaTech.Infrastructure.Auth;

namespace SampaTech.UnitTests.Infrastructure;

public class AuthOptionsValidatorTests
{
    private static AuthUser User(string username, string role = Roles.Manager, string hash = "hash") =>
        new() { Username = username, Role = role, PasswordHash = hash };

    private static IReadOnlyList<string> Errors(params AuthUser[] users)
    {
        var result = new AuthOptionsValidator().Validate(null, new AuthOptions { Users = [.. users] });
        return result.Succeeded ? [] : result.Failures!.ToList();
    }

    [Fact]
    public void Valid_configuration_passes()
    {
        Assert.Empty(Errors(User("gestor-matt"), User("ia-service", Roles.IaService), User("gestor-colega")));
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Manager)]
    [InlineData(Roles.Viewer)]
    [InlineData(Roles.IaService)]
    public void Every_known_role_is_accepted(string role)
    {
        Assert.Empty(Errors(User("usuario", role)));
    }

    [Theory]
    [InlineData("GESTOR-MATT")]
    [InlineData("gestor")]
    [InlineData("SUPER_ADMIN")]
    [InlineData("")]
    public void Unknown_role_is_refused_with_its_position(string role)
    {
        var errors = Errors(User("gestor-matt", role));

        var error = Assert.Single(errors);
        Assert.Contains("Auth:Users:0:Role", error);
    }

    [Fact]
    public void Empty_hash_and_empty_username_are_refused()
    {
        var errors = Errors(User("", hash: " "));

        Assert.Contains(errors, error => error.Contains(":Username"));
        Assert.Contains(errors, error => error.Contains(":PasswordHash"));
    }

    [Fact]
    public void Duplicated_username_is_refused_ignoring_case()
    {
        var errors = Errors(User("gestor-matt"), User("GESTOR-MATT"));

        Assert.Contains(errors, error => error.Contains("mais de uma vez"));
    }

    [Fact]
    public void No_users_is_refused()
    {
        Assert.Single(Errors());
    }
}
