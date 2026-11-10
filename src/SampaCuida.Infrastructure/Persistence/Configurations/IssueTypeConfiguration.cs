using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class IssueTypeConfiguration : IEntityTypeConfiguration<IssueType>
{
    public void Configure(EntityTypeBuilder<IssueType> builder)
    {
        builder.ToTable("ISSUE_TYPE");

        builder.HasKey(issueType => issueType.Id);
        builder.Property(issueType => issueType.Id)
            .HasColumnName("ID_ISSUE_TYPE");

        builder.Property(issueType => issueType.Name)
            .HasColumnName("NAME")
            .HasMaxLength(IssueType.NameMaxLength)
            .IsRequired();

        builder.HasIndex(issueType => issueType.Name)
            .IsUnique();

        builder.Property(issueType => issueType.Category)
            .HasColumnName("CATEGORY")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(issueType => issueType.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(IssueType.DescriptionMaxLength);

        builder.Property(issueType => issueType.IsActive)
            .HasColumnName("IS_ACTIVE");
    }
}
