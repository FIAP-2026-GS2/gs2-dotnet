using Microsoft.EntityFrameworkCore;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Idempotency;
using SampaTech.Infrastructure.Persistence;

namespace SampaTech.Infrastructure.Idempotency;

public class EfIdempotencyStore(AppDbContext db, TimeProvider timeProvider) : IIdempotencyStore
{
    public async Task<IdempotencyCheck> TryBeginAsync(
        string key, string requestHash, CancellationToken cancellationToken)
    {
        var existing = await db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(record => record.Key == key, cancellationToken);

        if (existing is not null)
            return Evaluate(existing, requestHash);

        var record = new IdempotencyRecord(key, requestHash, timeProvider.GetUtcNow().UtcDateTime);
        db.IdempotencyRecords.Add(record);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return IdempotencyCheck.Started;
        }
        catch (ConflictException)
        {
            // Outra requisição com a mesma chave reservou primeiro (violação da chave primária).
            return IdempotencyCheck.InProgress;
        }
        finally
        {
            // Solta o registro do contexto, para ele não se misturar com os dados da operação.
            db.Entry(record).State = EntityState.Detached;
        }
    }

    // ExecuteUpdate/ExecuteDelete vão direto ao banco, sem passar pelo controle de mudanças do EF.
    // Assim, se a operação falhou e deixou entidades pendentes no contexto, elas não são regravadas.
    public Task CompleteAsync(string key, int statusCode, string responseBody, CancellationToken cancellationToken) =>
        db.IdempotencyRecords
            .Where(record => record.Key == key)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(record => record.StatusCode, statusCode)
                .SetProperty(record => record.ResponseBody, responseBody), cancellationToken);

    public Task ReleaseAsync(string key, CancellationToken cancellationToken) =>
        db.IdempotencyRecords
            .Where(record => record.Key == key)
            .ExecuteDeleteAsync(cancellationToken);

    private static IdempotencyCheck Evaluate(IdempotencyRecord record, string requestHash)
    {
        if (record.RequestHash != requestHash)
            return IdempotencyCheck.Mismatch;

        if (record.StatusCode is null)
            return IdempotencyCheck.InProgress;

        return new IdempotencyCheck(IdempotencyCheckKind.Replay, record.StatusCode, record.ResponseBody);
    }
}
