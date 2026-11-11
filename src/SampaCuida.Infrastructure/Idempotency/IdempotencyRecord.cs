namespace SampaCuida.Infrastructure.Idempotency;

/// <summary>
/// Registro técnico de uma Idempotency-Key. Não faz parte do DER, porque não é dado
/// de negócio: só existe para a API reconhecer requisições repetidas.
/// </summary>
public class IdempotencyRecord
{
    public const int KeyMaxLength = 100;
    public const int RequestHashLength = 64;

    public string Key { get; private set; } = null!;

    // Impressão digital (SHA-256) do método, da rota e do corpo da requisição.
    public string RequestHash { get; private set; } = null!;

    // Nulos enquanto a requisição original ainda está sendo processada.
    public int? StatusCode { get; private set; }
    public string? ResponseBody { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private IdempotencyRecord() { }

    public IdempotencyRecord(string key, string requestHash, DateTime createdAt)
    {
        Key = key;
        RequestHash = requestHash;
        CreatedAt = createdAt;
    }
}
