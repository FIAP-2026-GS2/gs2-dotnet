using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SampaTech.Application.Abstractions;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Paging;
using SampaTech.Domain.Entities;
using SampaTech.Domain.Enums;

namespace SampaTech.Application.TriggerEvents;

// Datas como DateTimeOffset: aceitam qualquer fuso ("...-03:00" ou "...Z") e são
// convertidas para UTC no serviço. Sem data de início, vale o momento atual.
public record CreateTriggerEventRequest(
    TriggerEventType? EventType,
    string AlertSource,
    DateTimeOffset? StartDate,
    string? Description,
    string? Intensity);

public record UpdateTriggerEventRequest(string AlertSource, string? Description, string? Intensity);

public record EndTriggerEventRequest(DateTimeOffset? EndDate);

public record TriggerEventResponse(
    int Id,
    TriggerEventType EventType,
    string AlertSource,
    string? Description,
    string? Intensity,
    DateTime StartDate,
    DateTime? EndDate,
    TriggerEventStatus Status)
{
    public static TriggerEventResponse FromEntity(TriggerEvent triggerEvent) => new(
        triggerEvent.Id, triggerEvent.EventType, triggerEvent.AlertSource, triggerEvent.Description,
        triggerEvent.Intensity, triggerEvent.StartDate, triggerEvent.EndDate, triggerEvent.Status);
}

public record TriggerEventQuery(int Page = 1, int PageSize = 10, TriggerEventStatus? Status = null);

public class CreateTriggerEventRequestValidator : AbstractValidator<CreateTriggerEventRequest>
{
    public CreateTriggerEventRequestValidator()
    {
        RuleFor(x => x.EventType).NotNull().IsInEnum().WithName("tipo de evento");
        RuleFor(x => x.AlertSource).NotEmpty().MaximumLength(TriggerEvent.AlertSourceMaxLength).WithName("fonte do alerta");
        RuleFor(x => x.Description).MaximumLength(TriggerEvent.DescriptionMaxLength).WithName("descrição");
        RuleFor(x => x.Intensity).MaximumLength(TriggerEvent.IntensityMaxLength).WithName("intensidade");
    }
}

public class UpdateTriggerEventRequestValidator : AbstractValidator<UpdateTriggerEventRequest>
{
    public UpdateTriggerEventRequestValidator()
    {
        RuleFor(x => x.AlertSource).NotEmpty().MaximumLength(TriggerEvent.AlertSourceMaxLength).WithName("fonte do alerta");
        RuleFor(x => x.Description).MaximumLength(TriggerEvent.DescriptionMaxLength).WithName("descrição");
        RuleFor(x => x.Intensity).MaximumLength(TriggerEvent.IntensityMaxLength).WithName("intensidade");
    }
}

public class TriggerEventQueryValidator : AbstractValidator<TriggerEventQuery>
{
    public TriggerEventQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingExtensions.MaxPageSize).WithName("tamanho da página");
    }
}

public interface ITriggerEventService
{
    Task<PagedResponse<TriggerEventResponse>> ListAsync(TriggerEventQuery query, CancellationToken cancellationToken);
    Task<TriggerEventResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<TriggerEventResponse> CreateAsync(CreateTriggerEventRequest request, CancellationToken cancellationToken);
    Task<TriggerEventResponse> UpdateAsync(int id, UpdateTriggerEventRequest request, CancellationToken cancellationToken);
    Task<TriggerEventResponse> EndAsync(int id, EndTriggerEventRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

public class TriggerEventService(IAppDbContext db, TimeProvider timeProvider) : ITriggerEventService
{
    public async Task<PagedResponse<TriggerEventResponse>> ListAsync(
        TriggerEventQuery query, CancellationToken cancellationToken)
    {
        var events = db.TriggerEvents.AsNoTracking();

        if (query.Status is not null)
            events = events.Where(triggerEvent => triggerEvent.Status == query.Status);

        // Os mais recentes primeiro.
        return await events
            .OrderByDescending(triggerEvent => triggerEvent.StartDate)
            .ThenByDescending(triggerEvent => triggerEvent.Id)
            .ToPagedAsync(query.Page, query.PageSize, TriggerEventResponse.FromEntity, cancellationToken);
    }

    public async Task<TriggerEventResponse> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        TriggerEventResponse.FromEntity(await FindAsync(id, cancellationToken));

    public async Task<TriggerEventResponse> CreateAsync(
        CreateTriggerEventRequest request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate?.UtcDateTime ?? Now();

        var triggerEvent = new TriggerEvent(
            request.EventType!.Value, request.AlertSource, startDate, request.Description, request.Intensity);

        db.TriggerEvents.Add(triggerEvent);
        await db.SaveChangesAsync(cancellationToken);

        return TriggerEventResponse.FromEntity(triggerEvent);
    }

    public async Task<TriggerEventResponse> UpdateAsync(
        int id, UpdateTriggerEventRequest request, CancellationToken cancellationToken)
    {
        var triggerEvent = await FindAsync(id, cancellationToken);

        triggerEvent.UpdateDetails(request.AlertSource, request.Description, request.Intensity);

        await db.SaveChangesAsync(cancellationToken);
        return TriggerEventResponse.FromEntity(triggerEvent);
    }

    // Encerra o alerta (ex.: a Defesa Civil avisou que a chuva passou). Sem data, vale o momento atual.
    public async Task<TriggerEventResponse> EndAsync(
        int id, EndTriggerEventRequest request, CancellationToken cancellationToken)
    {
        var triggerEvent = await FindAsync(id, cancellationToken);

        triggerEvent.End(request.EndDate?.UtcDateTime ?? Now());

        await db.SaveChangesAsync(cancellationToken);
        return TriggerEventResponse.FromEntity(triggerEvent);
    }

    // Se o evento já gerou capturas, a FK recusa e a API responde 409.
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        db.TriggerEvents.Remove(await FindAsync(id, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;

    private async Task<TriggerEvent> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.TriggerEvents.FirstOrDefaultAsync(triggerEvent => triggerEvent.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Evento {id} não encontrado.");
}
