namespace SampaTech.Infrastructure.Auth;

/// <summary>Configuração "Jwt". A chave vem do user-secrets (local) ou de variável de ambiente (Azure).</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinKeyLength = 32; // HS256 exige chave de pelo menos 256 bits

    public string Issuer { get; set; } = "sampatech-api";
    public string Audience { get; set; } = "sampatech";
    public string Key { get; set; } = "";
    public int ExpirationMinutes { get; set; } = 60;

    // Emissores aceitos além do nosso (ex.: a API Java, na integração final).
    public List<string> AdditionalValidIssuers { get; set; } = [];
}

/// <summary>Configuração "Auth": os usuários do login, com a senha guardada como hash, nunca em texto.</summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    public List<AuthUser> Users { get; set; } = [];
}

public class AuthUser
{
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
}
