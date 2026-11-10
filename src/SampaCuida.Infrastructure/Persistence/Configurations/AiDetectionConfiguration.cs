using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class AiDetectionConfiguration : IEntityTypeConfiguration<AiDetection>
{
    public void Configure(EntityTypeBuilder<AiDetection> builder)
    {
        builder.ToTable("AI_DETECTION");

        builder.HasKey(detection => detection.Id);
        builder.Property(detection => detection.Id)
            .HasColumnName("ID_DETECTION");

        builder.Property(detection => detection.ConfidenceScore)
            .HasColumnName("CONFIDENCE_SCORE")
            .HasPrecision(5, 4);

        builder.Property(detection => detection.EstimatedSeverity)
            .HasColumnName("ESTIMATED_SEVERITY")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(detection => detection.DetectionCoordinates)
            .HasColumnName("DETECTION_COORDINATES")
            .HasMaxLength(AiDetection.CoordinatesMaxLength);

        builder.Property(detection => detection.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(AiDetection.DescriptionMaxLength);

        builder.Property(detection => detection.AiAnalysisId)
            .HasColumnName("ID_ANALYSIS");

        builder.Property(detection => detection.IssueTypeId)
            .HasColumnName("ID_ISSUE_TYPE");

        builder.Property(detection => detection.IssueReportId)
            .HasColumnName("ID_ISSUE_REPORT");

        builder.HasOne(detection => detection.IssueType)
            .WithMany()
            .HasForeignKey(detection => detection.IssueTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Opcional: a detecção só ganha ocorrência ao abrir uma ou ao ser deduplicada.
        builder.HasOne(detection => detection.IssueReport)
            .WithMany()
            .HasForeignKey(detection => detection.IssueReportId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
