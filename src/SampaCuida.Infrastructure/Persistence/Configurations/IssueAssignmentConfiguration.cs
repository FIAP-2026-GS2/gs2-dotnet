using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class IssueAssignmentConfiguration : IEntityTypeConfiguration<IssueAssignment>
{
    public void Configure(EntityTypeBuilder<IssueAssignment> builder)
    {
        builder.ToTable("ISSUE_ASSIGNMENT");

        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id)
            .HasColumnName("ID_ASSIGNMENT");

        builder.Property(assignment => assignment.AssignmentDate)
            .HasColumnName("ASSIGNMENT_DATE");

        builder.Property(assignment => assignment.ClosingDate)
            .HasColumnName("CLOSING_DATE");

        builder.Property(assignment => assignment.IssueReportId)
            .HasColumnName("ID_ISSUE_REPORT");

        builder.Property(assignment => assignment.TeamId)
            .HasColumnName("ID_TEAM");

        builder.HasOne(assignment => assignment.Team)
            .WithMany()
            .HasForeignKey(assignment => assignment.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        // IsOpen é calculado a partir da data de fechamento; não vira coluna.
        builder.Ignore(assignment => assignment.IsOpen);

        // Só uma atribuição aberta por ocorrência, garantido também pelo banco.
        builder.HasIndex(assignment => assignment.IssueReportId)
            .IsUnique()
            .HasDatabaseName("UX_ISSUE_ASSIGNMENT_OPEN")
            .HasFilter("[CLOSING_DATE] IS NULL");
    }
}
