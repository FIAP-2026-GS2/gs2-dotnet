using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

public class CameraConfiguration : IEntityTypeConfiguration<Camera>
{
    public void Configure(EntityTypeBuilder<Camera> builder)
    {
        builder.ToTable("Cameras");

        builder.HasKey(camera => camera.Id);

        builder.Property(camera => camera.Code)
            .HasMaxLength(Camera.CodeMaxLength)
            .IsRequired();

        builder.HasIndex(camera => camera.Code)
            .IsUnique();

        builder.Property(camera => camera.Description)
            .HasMaxLength(Camera.DescriptionMaxLength)
            .IsRequired();

        builder.Property(camera => camera.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(camera => camera.Address)
            .HasMaxLength(Camera.AddressMaxLength)
            .IsRequired();

        builder.Property(camera => camera.Neighborhood)
            .HasMaxLength(Camera.NeighborhoodMaxLength)
            .IsRequired();

        builder.Property(camera => camera.Latitude)
            .HasPrecision(9, 6);

        builder.Property(camera => camera.Longitude)
            .HasPrecision(9, 6);
    }
}
