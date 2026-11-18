using FluentValidation;
using SampaTech.Application.Common.Paging;
using SampaTech.Domain.Entities;
using SampaTech.Domain.Enums;

namespace SampaTech.Application.CitizenFeedbacks;

// Campos do formulário (a foto vem à parte, como arquivo).
public record SubmitCitizenFeedbackRequest(
    CitizenFeedbackDecision? Decision,
    string? Notes,
    decimal? Latitude,
    decimal? Longitude);

// Revisão da equipe: aprovar ou recusar (recusar exige motivo).
public record ReviewCitizenFeedbackRequest(bool? Approved, string? Notes);

public record CitizenFeedbackQuery(
    int Page = 1,
    int PageSize = 10,
    CitizenFeedbackStatus? Status = null,
    int? IssueReportId = null);

// O id do cidadão não sai na resposta: ela não carrega dados de pessoas.
public record CitizenFeedbackResponse(
    int Id,
    int IssueReportId,
    int CaptureId,
    string? PhotoUrl,
    CitizenFeedbackDecision Decision,
    CitizenFeedbackStatus Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? ReviewNotes,
    string Message);

public class SubmitCitizenFeedbackRequestValidator : AbstractValidator<SubmitCitizenFeedbackRequest>
{
    public SubmitCitizenFeedbackRequestValidator()
    {
        RuleFor(x => x.Decision).NotNull().IsInEnum().WithName("decisão");
        RuleFor(x => x.Notes).MaximumLength(CitizenFeedback.NotesMaxLength).WithName("observação");
        RuleFor(x => x.Notes).NotEmpty()
            .When(x => x.Decision == CitizenFeedbackDecision.CONTEST)
            .WithMessage("Informe o que continua errado para contestar a ocorrência.");
        RuleFor(x => x.Latitude).NotNull().InclusiveBetween(-90, 90).WithName("latitude");
        RuleFor(x => x.Longitude).NotNull().InclusiveBetween(-180, 180).WithName("longitude");
    }
}

public class ReviewCitizenFeedbackRequestValidator : AbstractValidator<ReviewCitizenFeedbackRequest>
{
    public ReviewCitizenFeedbackRequestValidator()
    {
        RuleFor(x => x.Approved).NotNull().WithName("aprovado");
        RuleFor(x => x.Notes).MaximumLength(CitizenFeedback.NotesMaxLength).WithName("observação");
        RuleFor(x => x.Notes).NotEmpty()
            .When(x => x.Approved == false)
            .WithMessage("Informe o motivo da recusa.");
    }
}

public class CitizenFeedbackQueryValidator : AbstractValidator<CitizenFeedbackQuery>
{
    public CitizenFeedbackQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingExtensions.MaxPageSize).WithName("tamanho da página");
        RuleFor(x => x.Status).IsInEnum().WithName("status");
    }
}
