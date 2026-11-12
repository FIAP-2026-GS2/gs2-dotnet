using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SampaTech.Application.Auth;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Infrastructure.Auth;

namespace SampaTech.UnitTests.Infrastructure;

public class JwtAuthServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 15, 0, 0, TimeSpan.Zero);
    private const string GestorPassword = "Gestor@2026";

    private static readonly JwtOptions Jwt = new()
    {
        Issuer = "sampatech-api",
        Audience = "sampatech",
        Key = new string('k', 48),
        ExpirationMinutes = 60
    };

    private static JwtAuthService CreateService(JwtOptions? jwt = null) => new(
        Options.Create(jwt ?? Jwt),
        Options.Create(new AuthOptions
        {
            Users = [new AuthUser { Username = "gestor", PasswordHash = PasswordHashing.Hash(GestorPassword), Role = Roles.Manager }]
        }),
        new FakeTimeProvider(Now));

    [Fact]
    public void Login_with_valid_credentials_returns_signed_token_with_role()
    {
        var response = CreateService().Login(new LoginRequest("gestor", GestorPassword));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(Roles.Manager, response.Role);
        Assert.Equal(Now.UtcDateTime.AddMinutes(60), response.ExpiresAt);
        Assert.Equal("gestor", token.Subject);
        Assert.Equal(Roles.Manager, token.Claims.Single(claim => claim.Type == "role").Value);
        Assert.Equal("sampatech-api", token.Issuer);
        Assert.Equal("HS256", token.Header.Alg);
    }

    [Fact]
    public void Username_is_case_insensitive()
    {
        Assert.Equal(Roles.Manager, CreateService().Login(new LoginRequest(" GESTOR ", GestorPassword)).Role);
    }

    [Theory]
    [InlineData("gestor", "senha-errada")]
    [InlineData("nao-existe", GestorPassword)]
    public void Wrong_password_or_unknown_user_gives_the_same_error(string username, string password)
    {
        var exception = Assert.Throws<UnauthorizedException>(() =>
            CreateService().Login(new LoginRequest(username, password)));

        Assert.Equal("Usuário ou senha inválidos.", exception.Message);
    }

    [Fact]
    public void Short_signing_key_is_refused()
    {
        var weak = new JwtOptions { Key = "curta" };

        Assert.Throws<InvalidOperationException>(() =>
            CreateService(weak).Login(new LoginRequest("gestor", GestorPassword)));
    }

    [Fact]
    public void Password_hash_never_contains_the_password_and_verifies_correctly()
    {
        var hash = PasswordHashing.Hash(GestorPassword);

        Assert.DoesNotContain(GestorPassword, hash);
        Assert.True(PasswordHashing.Verify(hash, GestorPassword));
        Assert.False(PasswordHashing.Verify(hash, "outra"));
        Assert.False(PasswordHashing.Verify("isto-nao-e-um-hash", GestorPassword));
    }
}
