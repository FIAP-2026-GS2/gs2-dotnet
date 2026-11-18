using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaTech.Domain.Entities;

namespace SampaTech.Infrastructure.Persistence.Configurations;

// Tabela nova da API .NET (não estava no DER): relatos de cidadãos com foto, revisados pela equipe.
// Nomes no mesmo padrão do DER do grupo.
public class CitizenFeedbackConfiguration : IEntityTypeConfiguration<CitizenFeedback>
{
    public void Configure(EntityTypeBuilder<CitizenFeedback> builder)
    {
        builder.ToTable("CITIZEN_FEEDBACK");

        builder.HasKey(feedback => feedback.Id);
        builder.Property(feedback => feedback.Id)
            .HasColumnName("ID_FEEDBACK");

        builder.Property(feedback => feedback.IssueReportId)
            .HasColumnName("ID_ISSUE_REPORT");

        builder.Property(feedback => feedback.CaptureId)
            .HasColumnName("ID_CAPTURE");

        // Cidadão que enviou (id do token); sem FK, como nas demais tabelas.
        builder.Property(feedback => feedback.UserId)
            .HasColumnName("ID_USER")
            .HasMaxLength(IssueReport.UserIdMaxLength)
            .IsRequired();

        builder.Property(feedback => feedback.Decision)
            .HasColumnName("DECISION")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(feedback => feedback.Status)
            .HasColumnName("STATUS")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(feedback => feedback.Notes)
            .HasColumnName("NOTES")
            .HasMaxLength(CitizenFeedback.NotesMaxLength);

        builder.Property(feedback => feedback.CreatedAt)
            .HasColumnName("CREATED_AT");

        builder.Property(feedback => feedback.ReviewedAt)
            .HasColumnName("REVIEWED_AT");

        builder.Property(feedback => feedback.ReviewedBy)
            .HasColumnName("REVIEWED_BY")
            .HasMaxLength(IssueReport.UserIdMaxLength);

        builder.Property(feedback => feedback.ReviewNotes)
            .HasColumnName("REVIEW_NOTES")
            .HasMaxLength(CitizenFeedback.NotesMaxLength);

        // Restrict: ocorrência e captura com relato não podem ser apagadas por baixo dele.
        builder.HasOne<IssueReport>()
            .WithMany()
            .HasForeignKey(feedback => feedback.IssueReportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(feedback => feedback.Capture)
            .WithMany()
            .HasForeignKey(feedback => feedback.CaptureId)
            .OnDelete(DeleteBehavior.Restrict);

        // A fila da equipe e a contagem por ocorrência filtram por estes dois campos.
        builder.HasIndex(feedback => new { feedback.IssueReportId, feedback.Status })
            .HasDatabaseName("IX_CITIZEN_FEEDBACK_ISSUE_STATUS");
    }
}
