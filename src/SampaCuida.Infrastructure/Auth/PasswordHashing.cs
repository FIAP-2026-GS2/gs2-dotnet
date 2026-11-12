using Microsoft.AspNetCore.Identity;

namespace SampaCuida.Infrastructure.Auth;

/// <summary>
/// Hash de senha com PBKDF2 (o mesmo algoritmo do ASP.NET Identity): guarda só a "impressão
/// digital" da senha, com sal e milhares de iterações. Quem ler a configuração não descobre a senha.
/// </summary>
public static class PasswordHashing
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object User = new();

    public static string Hash(string password) => Hasher.HashPassword(User, password);

    public static bool Verify(string passwordHash, string password)
    {
        try
        {
            return Hasher.VerifyHashedPassword(User, passwordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Hash mal formado na configuração: trata como senha errada.
            return false;
        }
    }
}
