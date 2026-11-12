using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaTech.Domain.Entities;

namespace SampaTech.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class IssueReportConfiguration : IEntityTypeConfiguration<IssueReport>
{
    public void Configure(EntityTypeBuilder<IssueReport> builder)
    {
        builder.ToTable("ISSUE_REPORT");

        builder.HasKey(report => report.Id);
        builder.Property(report => report.Id)
            .HasColumnName("ID_ISSUE_REPORT");

        builder.Property(report => report.Title)
            .HasColumnName("TITLE")
            .HasMaxLength(IssueReport.TitleMaxLength)
            .IsRequired();

        builder.Property(report => report.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(IssueReport.DescriptionMaxLength);

        builder.Property(report => report.Source)
            .HasColumnName("SOURCE")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(report => report.Severity)
            .HasColumnName("SEVERITY")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(report => report.Status)
            .HasColumnName("STATUS")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(report => report.OpeningDate)
            .HasColumnName("OPENING_DATE");

        builder.Property(report => report.ResolutionDate)
            .HasColumnName("RESOLUTION_DATE");

        builder.Property(report => report.Level)
            .HasColumnName("LEVEL");

        builder.Property(report => report.Side)
            .HasColumnName("SIDE")
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(report => report.IssueTypeId)
            .HasColumnName("ID_ISSUE_TYPE");

        // Id do usuário da API Java (outro banco): coluna simples, sem FK.
        builder.Property(report => report.UserId)
            .HasColumnName("ID_USER")
            .HasMaxLength(IssueReport.UserIdMaxLength);

        builder.HasOne(report => report.IssueType)
            .WithMany()
            .HasForeignKey(report => report.IssueTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // IsOpen é calculado a partir do Status; não vira coluna.
        builder.Ignore(report => report.IsOpen);

        // Agregado: histórico e atribuições pertencem à ocorrência e são gravados junto.
        builder.HasMany(report => report.History)
            .WithOne(history => history.IssueReport)
            .HasForeignKey(history => history.IssueReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(report => report.Assignments)
            .WithOne(assignment => assignment.IssueReport)
            .HasForeignKey(assignment => assignment.IssueReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(report => report.History)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(report => report.Assignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
