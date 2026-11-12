using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SampaTech.Application.Abstractions;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Domain.Entities;

namespace SampaTech.Application.CaptureSchedules;

// Horário no relógio de São Paulo ("12:00:00" ou "00:00:00"), e não em UTC.
public record CaptureScheduleRequest(
    TimeOnly? ExecutionTime,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DayOfWeek? Weekday);

public record CaptureScheduleResponse(
    int Id,
    int CameraId,
    TimeOnly ExecutionTime,
    DayOfWeek? Weekday,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive)
{
    public static CaptureScheduleResponse FromEntity(CaptureSchedule schedule) => new(
        schedule.Id, schedule.CameraId, schedule.ExecutionTime, schedule.Weekday, schedule.StartDate,
        schedule.EndDate, schedule.IsActive);
}

public class CaptureScheduleRequestValidator : AbstractValidator<CaptureScheduleRequest>
{
    public CaptureScheduleRequestValidator()
    {
        RuleFor(x => x.ExecutionTime).NotNull()
            .Must(time => time == CaptureSchedule.Noon || time == CaptureSchedule.Midnight)
            .WithMessage("O horário da agenda deve ser 12:00:00 ou 00:00:00.");
        RuleFor(x => x.StartDate).NotNull().WithName("início");
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate is not null && x.StartDate is not null)
            .WithMessage("O fim da agenda não pode ser anterior ao início.");
        RuleFor(x => x.Weekday).IsInEnum().WithName("dia da semana");
    }
}

/// <summary>Agendas pertencem a uma câmera: todas as rotas ficam em /api/cameras/{cameraId}/schedules.</summary>
public interface ICaptureScheduleService
{
    Task<IReadOnlyList<CaptureScheduleResponse>> ListAsync(int cameraId, CancellationToken cancellationToken);
    Task<CaptureScheduleResponse> GetByIdAsync(int cameraId, int id, CancellationToken cancellationToken);
    Task<CaptureScheduleResponse> CreateAsync(int cameraId, CaptureScheduleRequest request, CancellationToken cancellationToken);
    Task<CaptureScheduleResponse> UpdateAsync(int cameraId, int id, CaptureScheduleRequest request, CancellationToken cancellationToken);
    Task<CaptureScheduleResponse> SetActiveAsync(int cameraId, int id, bool active, CancellationToken cancellationToken);
    Task DeleteAsync(int cameraId, int id, CancellationToken cancellationToken);
}

public class CaptureScheduleService(IAppDbContext db) : ICaptureScheduleService
{
    // Uma câmera tem poucas agendas (no máximo meio-dia e meia-noite), então a lista não é paginada.
    public async Task<IReadOnlyList<CaptureScheduleResponse>> ListAsync(int cameraId, CancellationToken cancellationToken)
    {
        await EnsureCameraExistsAsync(cameraId, cancellationToken);

        var schedules = await db.CaptureSchedules.AsNoTracking()
            .Where(schedule => schedule.CameraId == cameraId)
            .OrderBy(schedule => schedule.Id)
            .ToListAsync(cancellationToken);

        return schedules.Select(CaptureScheduleResponse.FromEntity).ToList();
    }

    public async Task<CaptureScheduleResponse> GetByIdAsync(int cameraId, int id, CancellationToken cancellationToken) =>
        CaptureScheduleResponse.FromEntity(await FindAsync(cameraId, id, cancellationToken));

    public async Task<CaptureScheduleResponse> CreateAsync(
        int cameraId, CaptureScheduleRequest request, CancellationToken cancellationToken)
    {
        await EnsureCameraExistsAsync(cameraId, cancellationToken);

        var schedule = new CaptureSchedule(
            cameraId, request.ExecutionTime!.Value, request.StartDate!.Value, request.EndDate, request.Weekday);

        db.CaptureSchedules.Add(schedule);
        await db.SaveChangesAsync(cancellationToken);

        return CaptureScheduleResponse.FromEntity(schedule);
    }

    public async Task<CaptureScheduleResponse> UpdateAsync(
        int cameraId, int id, CaptureScheduleRequest request, CancellationToken cancellationToken)
    {
        var schedule = await FindAsync(cameraId, id, cancellationToken);

        schedule.Update(request.ExecutionTime!.Value, request.StartDate!.Value, request.EndDate, request.Weekday);

        await db.SaveChangesAsync(cancellationToken);
        return CaptureScheduleResponse.FromEntity(schedule);
    }

    public async Task<CaptureScheduleResponse> SetActiveAsync(
        int cameraId, int id, bool active, CancellationToken cancellationToken)
    {
        var schedule = await FindAsync(cameraId, id, cancellationToken);

        if (active) schedule.Activate();
        else schedule.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
        return CaptureScheduleResponse.FromEntity(schedule);
    }

    // Se a agenda já gerou capturas, a FK recusa e a API responde 409 (use a desativação).
    public async Task DeleteAsync(int cameraId, int id, CancellationToken cancellationToken)
    {
        db.CaptureSchedules.Remove(await FindAsync(cameraId, id, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    // Busca pela câmera e pelo id juntos: a agenda 5 da câmera 1 não aparece em /cameras/2/schedules/5.
    private async Task<CaptureSchedule> FindAsync(int cameraId, int id, CancellationToken cancellationToken) =>
        await db.CaptureSchedules.FirstOrDefaultAsync(
            schedule => schedule.Id == id && schedule.CameraId == cameraId, cancellationToken)
        ?? throw new NotFoundException($"Agenda {id} não encontrada na câmera {cameraId}.");

    private async Task EnsureCameraExistsAsync(int cameraId, CancellationToken cancellationToken)
    {
        if (!await db.Cameras.AnyAsync(camera => camera.Id == cameraId, cancellationToken))
            throw new NotFoundException($"Câmera {cameraId} não encontrada.");
    }
}
