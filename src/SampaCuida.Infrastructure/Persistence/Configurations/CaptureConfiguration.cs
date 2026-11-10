using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class CaptureConfiguration : IEntityTypeConfiguration<Capture>
{
    public void Configure(EntityTypeBuilder<Capture> builder)
    {
        builder.ToTable("CAPTURE");

        builder.HasKey(capture => capture.Id);
        builder.Property(capture => capture.Id)
            .HasColumnName("ID_CAPTURE");

        builder.Property(capture => capture.Source)
            .HasColumnName("SOURCE")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(capture => capture.Latitude)
            .HasColumnName("LATITUDE")
            .HasPrecision(9, 6);

        builder.Property(capture => capture.Longitude)
            .HasColumnName("LONGITUDE")
            .HasPrecision(9, 6);

        builder.Property(capture => capture.CaptureDate)
            .HasColumnName("CAPTURE_DATE");

        builder.Property(capture => capture.Status)
            .HasColumnName("STATUS")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(capture => capture.CameraId)
            .HasColumnName("ID_CAMERA");

        builder.Property(capture => capture.CaptureScheduleId)
            .HasColumnName("ID_SCHEDULE");

        builder.Property(capture => capture.TriggerEventId)
            .HasColumnName("ID_EVENT");

        // Id do usuário da API Java (outro banco): coluna simples, sem FK.
        builder.Property(capture => capture.UserId)
            .HasColumnName("ID_USER")
            .HasMaxLength(Capture.UserIdMaxLength);

        builder.HasOne(capture => capture.Camera)
            .WithMany()
            .HasForeignKey(capture => capture.CameraId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(capture => capture.CaptureSchedule)
            .WithMany()
            .HasForeignKey(capture => capture.CaptureScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(capture => capture.TriggerEvent)
            .WithMany()
            .HasForeignKey(capture => capture.TriggerEventId)
            .OnDelete(DeleteBehavior.Restrict);

        // Idempotência no banco: uma câmera não gera dois frames do mesmo instante.
        // O filtro aplica a regra só às capturas de câmera; sem ele, o SQL Server
        // trataria todos os NULL (fotos de cidadão) como iguais.
        builder.HasIndex(capture => new { capture.CameraId, capture.CaptureDate })
            .IsUnique()
            .HasDatabaseName("UX_CAPTURE_CAMERA_DATE")
            .HasFilter("[ID_CAMERA] IS NOT NULL");
    }
}
