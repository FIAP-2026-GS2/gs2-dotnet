using FluentValidation;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;

namespace SampaCuida.Application.IssueReports;

public class IssueReportQueryValidator : AbstractValidator<IssueReportQuery>
{
    public IssueReportQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingExtensions.MaxPageSize).WithName("tamanho da página");
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("O fim do período não pode ser anterior ao início.");
    }
}

public class PublicIssueReportQueryValidator : AbstractValidator<PublicIssueReportQuery>
{
    public PublicIssueReportQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingExtensions.MaxPageSize).WithName("tamanho da página");
        // O cidadão não vê os falsos positivos descartados pela prefeitura.
        RuleFor(x => x.Status).NotEqual(IssueStatus.REJECTED)
            .WithMessage("Ocorrências rejeitadas não são públicas.");
    }
}

public class ChangeIssueStatusRequestValidator : AbstractValidator<ChangeIssueStatusRequest>
{
    public ChangeIssueStatusRequestValidator()
    {
        RuleFor(x => x.Status).NotNull().IsInEnum().WithName("status");
        RuleFor(x => x.Status).NotEqual(IssueStatus.ASSIGNED)
            .WithMessage("Para atribuir uma equipe, use POST /api/issue-reports/{id}/assignments.");
        RuleFor(x => x.Notes).MaximumLength(IssueHistory.NotesMaxLength).WithName("observação");
        RuleFor(x => x.Notes).NotEmpty()
            .When(x => x.Status == IssueStatus.REJECTED)
            .WithMessage("Informe o motivo da rejeição.");
    }
}

public class CreateAssignmentRequestValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentRequestValidator()
    {
        RuleFor(x => x.TeamId).NotNull().GreaterThan(0).WithName("equipe");
    }
}
