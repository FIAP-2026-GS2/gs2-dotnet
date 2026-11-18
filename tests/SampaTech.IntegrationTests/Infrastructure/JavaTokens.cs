using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SampaTech.IntegrationTests.Infrastructure;

/// <summary>
/// Gera tokens no mesmo formato da API Java: sem "iss" e sem "aud", "sub" = e-mail em minúsculas,
/// "role" e "name", assinados com o segredo em texto puro. É assim que o app do cidadão chega na .NET.
/// </summary>
public static class JavaTokens
{
    // 64 caracteres: com essa chave a Java assina em HS512 (o algoritmo sai do tamanho do segredo).
    public const string Secret = "segredo-de-teste-da-api-java-com-64-caracteres-0123456789abcdefg";

    public static string Create(
        string email,
        string role = "CITIZEN",
        string algorithm = SecurityAlgorithms.HmacSha512,
        string secret = Secret,
        DateTime? expires = null)
    {
        // Como na Java: "iat" e "exp" (120 min), sem "nbf", "iss" e "aud".
        var issuedAt = DateTime.UtcNow;
        var expiresAt = expires ?? issuedAt.AddMinutes(120);
        var header = new JwtHeader(
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), algorithm));
        var payload = new JwtPayload
        {
            ["sub"] = email,
            ["role"] = role,
            ["name"] = "Cidadão de Teste",
            ["iat"] = EpochTime.GetIntDate(expiresAt.AddMinutes(-120)),
            ["exp"] = EpochTime.GetIntDate(expiresAt)
        };

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));
    }

    public static HttpClient Client(this ApiFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
