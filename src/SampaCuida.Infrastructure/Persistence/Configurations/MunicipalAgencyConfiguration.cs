using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class MunicipalAgencyConfiguration : IEntityTypeConfiguration<MunicipalAgency>
{
    public void Configure(EntityTypeBuilder<MunicipalAgency> builder)
    {
        builder.ToTable("MUNICIPAL_AGENCY");

        builder.HasKey(agency => agency.Id);
        builder.Property(agency => agency.Id)
            .HasColumnName("ID_AGENCY");

        builder.Property(agency => agency.Name)
            .HasColumnName("NAME")
            .HasMaxLength(MunicipalAgency.NameMaxLength)
            .IsRequired();

        builder.Property(agency => agency.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(MunicipalAgency.DescriptionMaxLength);

        builder.Property(agency => agency.City)
            .HasColumnName("CITY")
            .HasMaxLength(MunicipalAgency.CityMaxLength)
            .IsRequired();

        builder.Property(agency => agency.State)
            .HasColumnName("STATE")
            .HasMaxLength(2)
            .IsFixedLength()
            .IsRequired();

        builder.Property(agency => agency.IsActive)
            .HasColumnName("IS_ACTIVE");

        builder.Property(agency => agency.Subprefecture)
            .HasColumnName("SUBPREFECTURE")
            .HasMaxLength(MunicipalAgency.SubprefectureMaxLength);
    }
}
