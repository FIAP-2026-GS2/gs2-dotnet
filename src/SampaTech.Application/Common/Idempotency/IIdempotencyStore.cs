namespace SampaTech.Application.Common.Idempotency;

public enum IdempotencyCheckKind
{
    Started,     // chave nova: reservada, pode executar a operação
    Replay,      // mesma chave e mesmo conteúdo: devolver a resposta guardada
    Mismatch,    // mesma chave com outro conteúdo: erro de quem chamou (422)
    InProgress   // mesma chave ainda sendo processada por outra requisição (409)
}

public record IdempotencyCheck(IdempotencyCheckKind Kind, int? StatusCode = null, string? ResponseBody = null)
{
    public static readonly IdempotencyCheck Started = new(IdempotencyCheckKind.Started);
    public static readonly IdempotencyCheck Mismatch = new(IdempotencyCheckKind.Mismatch);
    public static readonly IdempotencyCheck InProgress = new(IdempotencyCheckKind.InProgress);
}

/// <summary>
/// Onde ficam as chaves de idempotência já usadas e as respostas dadas.
/// A implementação (tabela IDEMPOTENCY_RECORD) fica no Infrastructure.
/// </summary>
public interface IIdempotencyStore
{
    // Reserva a chave ou informa o que já existe para ela.
    Task<IdempotencyCheck> TryBeginAsync(string key, string requestHash, CancellationToken cancellationToken);

    // Guarda a resposta de sucesso, para devolvê-la nas repetições.
    Task CompleteAsync(string key, int statusCode, string responseBody, CancellationToken cancellationToken);

    // Libera a chave quando a operação falha, para a nova tentativa poder funcionar.
    Task ReleaseAsync(string key, CancellationToken cancellationToken);
}
