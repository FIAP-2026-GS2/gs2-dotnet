using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SampaCuida.Application.Abstractions;
using SampaCuida.Application.Common.Exceptions;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;

namespace SampaCuida.Application.IssueTypes;

public record IssueTypeRequest(string Name, IssueCategory? Category, string? Description);

public record IssueTypeResponse(int Id, string Name, IssueCategory Category, string? Description, bool IsActive)
{
    public static IssueTypeResponse FromEntity(IssueType issueType) => new(
        issueType.Id, issueType.Name, issueType.Category, issueType.Description, issueType.IsActive);
}

public record IssueTypeQuery(int Page = 1, int PageSize = 10, IssueCategory? Category = null, bool? IsActive = null);

public class IssueTypeRequestValidator : AbstractValidator<IssueTypeRequest>
{
    public IssueTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(IssueType.NameMaxLength).WithName("nome");
        RuleFor(x => x.Category).NotNull().IsInEnum().WithName("categoria");
        RuleFor(x => x.Description).MaximumLength(IssueType.DescriptionMaxLength).WithName("descrição");
    }
}

public class IssueTypeQueryValidator : AbstractValidator<IssueTypeQuery>
{
    public IssueTypeQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingExtensions.MaxPageSize).WithName("tamanho da página");
    }
}

public interface IIssueTypeService
{
    Task<PagedResponse<IssueTypeResponse>> ListAsync(IssueTypeQuery query, CancellationToken cancellationToken);
    Task<IssueTypeResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IssueTypeResponse> CreateAsync(IssueTypeRequest request, CancellationToken cancellationToken);
    Task<IssueTypeResponse> UpdateAsync(int id, IssueTypeRequest request, CancellationToken cancellationToken);
    Task<IssueTypeResponse> SetActiveAsync(int id, bool active, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

public class IssueTypeService(IAppDbContext db) : IIssueTypeService
{
    public async Task<PagedResponse<IssueTypeResponse>> ListAsync(IssueTypeQuery query, CancellationToken cancellationToken)
    {
        var issueTypes = db.IssueTypes.AsNoTracking();

        if (query.Category is not null)
            issueTypes = issueTypes.Where(issueType => issueType.Category == query.Category);

        if (query.IsActive is not null)
            issueTypes = issueTypes.Where(issueType => issueType.IsActive == query.IsActive);

        return await issueTypes
            .OrderBy(issueType => issueType.Id)
            .ToPagedAsync(query.Page, query.PageSize, IssueTypeResponse.FromEntity, cancellationToken);
    }

    public async Task<IssueTypeResponse> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        IssueTypeResponse.FromEntity(await FindAsync(id, cancellationToken));

    // Nome repetido é barrado pelo índice único (409).
    public async Task<IssueTypeResponse> CreateAsync(IssueTypeRequest request, CancellationToken cancellationToken)
    {
        var issueType = new IssueType(request.Name, request.Category!.Value, request.Description);

        db.IssueTypes.Add(issueType);
        await db.SaveChangesAsync(cancellationToken);

        return IssueTypeResponse.FromEntity(issueType);
    }

    public async Task<IssueTypeResponse> UpdateAsync(int id, IssueTypeRequest request, CancellationToken cancellationToken)
    {
        var issueType = await FindAsync(id, cancellationToken);

        issueType.Update(request.Name, request.Category!.Value, request.Description);

        await db.SaveChangesAsync(cancellationToken);
        return IssueTypeResponse.FromEntity(issueType);
    }

    public async Task<IssueTypeResponse> SetActiveAsync(int id, bool active, CancellationToken cancellationToken)
    {
        var issueType = await FindAsync(id, cancellationToken);

        if (active) issueType.Activate();
        else issueType.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
        return IssueTypeResponse.FromEntity(issueType);
    }

    // Se o tipo já é usado por detecções ou ocorrências, a FK recusa e a API responde 409.
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        db.IssueTypes.Remove(await FindAsync(id, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IssueType> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.IssueTypes.FirstOrDefaultAsync(issueType => issueType.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Tipo de problema {id} não encontrado.");
}
