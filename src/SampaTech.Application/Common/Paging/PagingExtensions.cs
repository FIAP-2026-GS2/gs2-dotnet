using Microsoft.EntityFrameworkCore;

namespace SampaTech.Application.Common.Paging;

public static class PagingExtensions
{
    public const int MaxPageSize = 50;

    // Conta o total, busca só a página pedida e converte cada item em DTO.
    public static async Task<PagedResponse<TResponse>> ToPagedAsync<TEntity, TResponse>(
        this IQueryable<TEntity> query,
        int page,
        int pageSize,
        Func<TEntity, TResponse> map,
        CancellationToken cancellationToken)
    {
        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TResponse>(items.Select(map).ToList(), page, pageSize, totalItems);
    }
}
