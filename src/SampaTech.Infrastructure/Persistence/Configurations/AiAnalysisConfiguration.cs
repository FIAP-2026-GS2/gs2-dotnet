using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaTech.Domain.Entities;

namespace SampaTech.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class AiAnalysisConfiguration : IEntityTypeConfiguration<AiAnalysis>
{
    public void Configure(EntityTypeBuilder<AiAnalysis> builder)
    {
        builder.ToTable("AI_ANALYSIS");

        builder.HasKey(analysis => analysis.Id);
        builder.Property(analysis => analysis.Id)
            .HasColumnName("ID_ANALYSIS");

        builder.Property(analysis => analysis.AiModel)
            .HasColumnName("AI_MODEL")
            .HasMaxLength(AiAnalysis.AiModelMaxLength)
            .IsRequired();

        builder.Property(analysis => analysis.ModelVersion)
            .HasColumnName("MODEL_VERSION")
            .HasMaxLength(AiAnalysis.ModelVersionMaxLength);

        builder.Property(analysis => analysis.Status)
            .HasColumnName("STATUS")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(analysis => analysis.AnalysisDate)
            .HasColumnName("ANALYSIS_DATE");

        builder.Property(analysis => analysis.ProcessingTimeMs)
            .HasColumnName("PROCESSING_TIME_MS");

        builder.Property(analysis => analysis.ImageId)
            .HasColumnName("ID_IMAGE");

        builder.HasOne(analysis => analysis.Image)
            .WithMany()
            .HasForeignKey(analysis => analysis.ImageId)
            .OnDelete(DeleteBehavior.Restrict);

        // Agregado: as detecções pertencem à análise e são gravadas junto com ela.
        // O EF escreve na lista privada _detections (a pública é só leitura).
        builder.HasMany(analysis => analysis.Detections)
            .WithOne(detection => detection.AiAnalysis)
            .HasForeignKey(detection => detection.AiAnalysisId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(analysis => analysis.Detections)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
