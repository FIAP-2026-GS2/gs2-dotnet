using FluentValidation;

namespace SampaTech.Application.Auth;

/// <summary>
/// Papéis da API (mesmos nomes do token, combinados com a API Java). Três níveis para pessoas,
/// cada um com algo que o de baixo não pode, e um papel à parte para o serviço de IA.
/// </summary>
public static class Roles
{
    public const string Admin = "ADMIN";          // responsável: pode tudo, inclusive apagar de verdade
    public const string Manager = "MANAGER";      // gestão do dia a dia: cadastra, edita, atribui, muda status
    public const string Viewer = "VIEWER";        // só consulta
    public const string IaService = "IA_SERVICE"; // o serviço de IA em Python (não é pessoa)

    // Únicos valores aceitos no campo Role da configuração (conferido na subida da API).
    public static readonly IReadOnlyList<string> All = [Admin, Manager, Viewer, IaService];
}

/// <summary>
/// Políticas em escada: cada rota pede a política mínima que precisa, e um nível de cima
/// herda tudo dos de baixo (o ADMIN passa em Read, Manage e Delete).
/// </summary>
public static class AuthPolicies
{
    public const string Read = "Read";           // ADMIN, MANAGER, VIEWER
    public const string Manage = "Manage";       // ADMIN, MANAGER
    public const string Delete = "Delete";       // só ADMIN (exclusão é irreversível)
    public const string IaService = "IaService"; // só IA_SERVICE
}

public record LoginRequest(string Username, string Password);

public record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAt, string Role);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100).WithName("usuário");
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200).WithName("senha");
    }
}

/// <summary>Confere usuário e senha e emite o token. A implementação fica no Infrastructure.</summary>
public interface IAuthService
{
    LoginResponse Login(LoginRequest request);
}

/// <summary>
/// Quem está chamando a API (o "sub" do token). Usado para registrar no histórico
/// quem mudou o status ou atribuiu a equipe. A implementação lê o token da requisição (Api).
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
}
