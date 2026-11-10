using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class CameraConfiguration : IEntityTypeConfiguration<Camera>
{
    public void Configure(EntityTypeBuilder<Camera> builder)
    {
        builder.ToTable("CAMERA");

        builder.HasKey(camera => camera.Id);
        builder.Property(camera => camera.Id)
            .HasColumnName("ID_CAMERA");

        builder.Property(camera => camera.Code)
            .HasColumnName("CAMERA_CODE")
            .HasMaxLength(Camera.CodeMaxLength)
            .IsRequired();

        builder.HasIndex(camera => camera.Code)
            .IsUnique();

        builder.Property(camera => camera.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(Camera.DescriptionMaxLength);

        builder.Property(camera => camera.Status)
            .HasColumnName("STATUS")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(camera => camera.InstallationDate)
            .HasColumnName("INSTALLATION_DATE");

        builder.Property(camera => camera.MonitoringPointId)
            .HasColumnName("ID_POINT");

        // Restrict: o banco recusa apagar um ponto que ainda tem câmeras,
        // em vez de apagar as câmeras (e o histórico delas) junto.
        builder.HasOne(camera => camera.MonitoringPoint)
            .WithMany()
            .HasForeignKey(camera => camera.MonitoringPointId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
