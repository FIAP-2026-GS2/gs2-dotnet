using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("TEAM");

        builder.HasKey(team => team.Id);
        builder.Property(team => team.Id)
            .HasColumnName("ID_TEAM");

        builder.Property(team => team.Name)
            .HasColumnName("NAME")
            .HasMaxLength(Team.NameMaxLength)
            .IsRequired();

        builder.Property(team => team.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(Team.DescriptionMaxLength);

        builder.Property(team => team.IsActive)
            .HasColumnName("IS_ACTIVE");

        builder.Property(team => team.MunicipalAgencyId)
            .HasColumnName("ID_AGENCY");

        // Restrict: o banco recusa apagar um órgão que ainda tem equipes.
        builder.HasOne(team => team.MunicipalAgency)
            .WithMany()
            .HasForeignKey(team => team.MunicipalAgencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
