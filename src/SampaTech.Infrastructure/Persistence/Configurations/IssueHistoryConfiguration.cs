using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaTech.Domain.Entities;

namespace SampaTech.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class IssueHistoryConfiguration : IEntityTypeConfiguration<IssueHistory>
{
    public void Configure(EntityTypeBuilder<IssueHistory> builder)
    {
        builder.ToTable("ISSUE_HISTORY");

        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id)
            .HasColumnName("ID_HISTORY");

        builder.Property(history => history.PreviousStatus)
            .HasColumnName("PREVIOUS_STATUS")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(history => history.NewStatus)
            .HasColumnName("NEW_STATUS")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(history => history.Notes)
            .HasColumnName("NOTES")
            .HasMaxLength(IssueHistory.NotesMaxLength);

        builder.Property(history => history.ChangeDate)
            .HasColumnName("CHANGE_DATE");

        builder.Property(history => history.IssueReportId)
            .HasColumnName("ID_ISSUE_REPORT");

        // Quem mudou o status (id do token); sem FK.
        builder.Property(history => history.UserId)
            .HasColumnName("ID_USER")
            .HasMaxLength(IssueReport.UserIdMaxLength);
    }
}
