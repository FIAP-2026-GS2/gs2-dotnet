using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class CaptureScheduleConfiguration : IEntityTypeConfiguration<CaptureSchedule>
{
    public void Configure(EntityTypeBuilder<CaptureSchedule> builder)
    {
        builder.ToTable("CAPTURE_SCHEDULE");

        builder.HasKey(schedule => schedule.Id);
        builder.Property(schedule => schedule.Id)
            .HasColumnName("ID_SCHEDULE");

        builder.Property(schedule => schedule.Weekday)
            .HasColumnName("WEEKDAY")
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(schedule => schedule.ExecutionTime)
            .HasColumnName("EXECUTION_TIME");

        builder.Property(schedule => schedule.StartDate)
            .HasColumnName("START_DATE");

        builder.Property(schedule => schedule.EndDate)
            .HasColumnName("END_DATE");

        builder.Property(schedule => schedule.IsActive)
            .HasColumnName("IS_ACTIVE");

        builder.Property(schedule => schedule.CameraId)
            .HasColumnName("ID_CAMERA");

        builder.HasOne(schedule => schedule.Camera)
            .WithMany()
            .HasForeignKey(schedule => schedule.CameraId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
