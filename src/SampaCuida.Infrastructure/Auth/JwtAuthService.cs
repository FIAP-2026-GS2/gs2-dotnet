using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SampaCuida.Application.Auth;
using SampaCuida.Application.Common.Exceptions;

namespace SampaCuida.Infrastructure.Auth;

/// <summary>Confere usuário e senha (hash) e emite o token JWT assinado com HS256.</summary>
public class JwtAuthService(
    IOptions<JwtOptions> jwtOptions, IOptions<AuthOptions> authOptions, TimeProvider timeProvider) : IAuthService
{
    // Hash usado quando o usuário não existe: a verificação leva o mesmo tempo nos dois casos,
    // e quem tenta adivinhar não descobre, pelo tempo de resposta, quais usuários existem.
    private static readonly string DummyHash = PasswordHashing.Hash(Guid.NewGuid().ToString());

    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly AuthOptions _auth = authOptions.Value;

    public LoginResponse Login(LoginRequest request)
    {
        var user = _auth.Users.FirstOrDefault(user =>
            string.Equals(user.Username, request.Username.Trim(), StringComparison.OrdinalIgnoreCase));

        var passwordOk = PasswordHashing.Verify(user?.PasswordHash ?? DummyHash, request.Password);

        // Mesma mensagem para usuário inexistente e senha errada: não revela qual dos dois falhou.
        if (user is null || !passwordOk)
            throw new UnauthorizedException("Usuário ou senha inválidos.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_jwt.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim("role", user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            notBefore: now,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(SigningKey(_jwt), SecurityAlgorithms.HmacSha256));

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", expiresAt, user.Role);
    }

    // A mesma chave assina (aqui) e confere (na validação do token, na Api).
    public static SymmetricSecurityKey SigningKey(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Key) || options.Key.Length < JwtOptions.MinKeyLength)
            throw new InvalidOperationException(
                $"A chave 'Jwt:Key' não foi configurada ou tem menos de {JwtOptions.MinKeyLength} caracteres.");

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
    }
}
