using Microsoft.Extensions.DependencyInjection;
using SampaTech.Application;
using SampaTech.Application.IssueReports;
using SampaTech.Domain.Enums;

namespace SampaTech.UnitTests.Application;

public class IssueReportValidatorTests
{
    // Aplica a mesma configuração da API (mensagens em português e chaves em camelCase).
    static IssueReportValidatorTests() => new ServiceCollection().AddApplication();

    private static bool IsValid(ChangeIssueStatusRequest request) =>
        new ChangeIssueStatusRequestValidator().Validate(request).IsValid;

    [Theory]
    [InlineData(IssueStatus.IN_PROGRESS)]
    [InlineData(IssueStatus.RESOLVED)]
    public void Status_change_accepts_valid_target(IssueStatus status)
    {
        Assert.True(IsValid(new ChangeIssueStatusRequest(status, Notes: null)));
    }

    [Fact]
    public void Status_change_to_assigned_must_use_the_assignment_route()
    {
        Assert.False(IsValid(new ChangeIssueStatusRequest(IssueStatus.ASSIGNED, null)));
    }

    [Fact]
    public void Rejection_requires_a_reason()
    {
        Assert.False(IsValid(new ChangeIssueStatusRequest(IssueStatus.REJECTED, Notes: " ")));
        Assert.True(IsValid(new ChangeIssueStatusRequest(IssueStatus.REJECTED, "Sombra de árvore, não é buraco.")));
    }

    [Fact]
    public void Assignment_requires_team()
    {
        Assert.False(new CreateAssignmentRequestValidator().Validate(new CreateAssignmentRequest(null)).IsValid);
    }

    [Fact]
    public void Public_query_cannot_ask_for_rejected_reports()
    {
        var result = new PublicIssueReportQueryValidator()
            .Validate(new PublicIssueReportQuery(Status: IssueStatus.REJECTED));

        Assert.False(result.IsValid);
    }
}
