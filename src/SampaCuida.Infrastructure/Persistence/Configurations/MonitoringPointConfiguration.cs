using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class MonitoringPointConfiguration : IEntityTypeConfiguration<MonitoringPoint>
{
    public void Configure(EntityTypeBuilder<MonitoringPoint> builder)
    {
        builder.ToTable("MONITORING_POINT");

        builder.HasKey(point => point.Id);
        builder.Property(point => point.Id)
            .HasColumnName("ID_POINT");

        builder.Property(point => point.Street)
            .HasColumnName("STREET")
            .HasMaxLength(MonitoringPoint.StreetMaxLength)
            .IsRequired();

        builder.Property(point => point.StreetNumber)
            .HasColumnName("STREET_NUMBER")
            .HasMaxLength(MonitoringPoint.StreetNumberMaxLength);

        builder.Property(point => point.Neighborhood)
            .HasColumnName("NEIGHBORHOOD")
            .HasMaxLength(MonitoringPoint.NeighborhoodMaxLength);

        builder.Property(point => point.City)
            .HasColumnName("CITY")
            .HasMaxLength(MonitoringPoint.CityMaxLength)
            .IsRequired();

        builder.Property(point => point.State)
            .HasColumnName("STATE")
            .HasMaxLength(MonitoringPoint.StateLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(point => point.Latitude)
            .HasColumnName("LATITUDE")
            .HasPrecision(9, 6);

        builder.Property(point => point.Longitude)
            .HasColumnName("LONGITUDE")
            .HasPrecision(9, 6);

        builder.Property(point => point.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(MonitoringPoint.DescriptionMaxLength);
    }
}
