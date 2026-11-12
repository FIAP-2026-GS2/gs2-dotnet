using Microsoft.Extensions.Options;
using SampaCuida.Application.Auth;

namespace SampaCuida.Infrastructure.Auth;

/// <summary>
/// Confere os usuários da configuração na subida da API. Se algo estiver errado, a API não sobe
/// e a mensagem diz qual posição corrigir (ex.: papel "GESTOR-MATT", que não existe).
/// Melhor falhar cedo e com clareza do que subir e devolver um 403 confuso depois.
/// </summary>
public class AuthOptionsValidator : IValidateOptions<AuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        var errors = new List<string>();

        if (options.Users.Count == 0)
            errors.Add("Nenhum usuário configurado em Auth:Users: ninguém conseguiria fazer login.");

        for (var index = 0; index < options.Users.Count; index++)
        {
            var user = options.Users[index];
            var position = $"Auth:Users:{index}";

            if (string.IsNullOrWhiteSpace(user.Username))
                errors.Add($"{position}:Username está vazio.");

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                errors.Add($"{position}:PasswordHash está vazio (gere com: dotnet run --project src/SampaCuida.Api -- hash-password 'senha').");

            if (!Roles.All.Contains(user.Role))
                errors.Add($"{position}:Role é '{user.Role}', mas só aceita: {string.Join(", ", Roles.All)} (exatamente assim).");
        }

        var duplicated = options.Users
            .Where(user => !string.IsNullOrWhiteSpace(user.Username))
            .GroupBy(user => user.Username.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var username in duplicated)
            errors.Add($"O usuário '{username}' aparece mais de uma vez em Auth:Users.");

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
