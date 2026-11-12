using SampaTech.Domain.Common;
using SampaTech.Domain.Exceptions;

namespace SampaTech.Domain.Entities;

/// <summary>Equipe atribuída a uma ocorrência. Criada e fechada só pela própria ocorrência.</summary>
public class IssueAssignment
{
    public int Id { get; private set; }
    public int IssueReportId { get; private set; }
    public IssueReport? IssueReport { get; private set; }
    public int TeamId { get; private set; }
    public Team? Team { get; private set; }
    public DateTime AssignmentDate { get; private set; }
    public DateTime? ClosingDate { get; private set; }

    public bool IsOpen => ClosingDate is null;

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private IssueAssignment() { }

    internal IssueAssignment(IssueReport issueReport, int teamId, DateTime assignmentDate)
    {
        IssueReport = issueReport;
        TeamId = Guard.RequiredId(teamId, "equipe");
        AssignmentDate = Guard.Utc(assignmentDate, "atribuição");
    }

    internal void Close(DateTime closingDate)
    {
        if (closingDate < AssignmentDate)
            throw new DomainException("O fechamento da atribuição não pode ser anterior à atribuição.");

        ClosingDate = closingDate;
    }
}
