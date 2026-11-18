using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using SampaTech.Application.Auth;

namespace SampaTech.Api.Auth;

/// <summary>
/// Token do app do cidadão, emitido pela API Java (portal). Ele não tem emissor ("iss") nem público ("aud"),
/// "sub" é o e-mail e "role" é CITIZEN, AGENT ou ADMIN. A .NET aceita dele só o papel CITIZEN.
/// </summary>
public static class JavaPortalToken
{
    public const string Scheme = "JavaPortal";

    // Esquema de entrada: olha o token e manda para o validador certo (o da .NET ou o da Java).
    public const string SelectorScheme = "SampaTechOrJava";

    private const string UserIdPrefix = "java:";

    /// <summary>
    /// Só olha (não valida): o token da .NET sempre tem emissor; o da Java nunca tem. A validação de verdade
    /// (assinatura, validade e papel) acontece depois, no esquema escolhido.
    /// </summary>
    public static bool LooksLikeJavaToken(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = header["Bearer ".Length..].Trim();
        var handler = new JsonWebTokenHandler();
        if (!handler.CanReadToken(token))
            return false;

        try
        {
            return string.IsNullOrEmpty(handler.ReadJsonWebToken(token).Issuer);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Depois da assinatura conferida: recusa quem não é CITIZEN e troca o e-mail por um código.
    /// O código (SHA-256 do e-mail) é sempre o mesmo para a mesma pessoa, então ela continua vendo só os
    /// próprios relatos, mas o e-mail não fica gravado na .NET (o cadastro está na Java).
    /// </summary>
    public static Task OnTokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal!;
        var role = principal.FindFirst("role")?.Value;
        var email = principal.FindFirst("sub")?.Value;

        if (role != Roles.Citizen)
        {
            context.Fail($"Token da API Java com papel '{role}': a API .NET aceita da Java só o papel CITIZEN.");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            context.Fail("Token da API Java sem o e-mail (sub).");
            return Task.CompletedTask;
        }

        var identity = new ClaimsIdentity(
            [new Claim("sub", UserIdFor(email)), new Claim("role", Roles.Citizen)],
            authenticationType: Scheme, nameType: "sub", roleType: "role");

        context.Principal = new ClaimsPrincipal(identity);
        return Task.CompletedTask;
    }

    /// <summary>"java:" + SHA-256 do e-mail em minúsculas (69 caracteres, cabe no ID_USER).</summary>
    public static string UserIdFor(string email)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));
        return UserIdPrefix + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
