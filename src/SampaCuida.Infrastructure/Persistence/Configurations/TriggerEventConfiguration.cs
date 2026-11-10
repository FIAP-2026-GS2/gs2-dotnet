using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class TriggerEventConfiguration : IEntityTypeConfiguration<TriggerEvent>
{
    public void Configure(EntityTypeBuilder<TriggerEvent> builder)
    {
        builder.ToTable("TRIGGER_EVENT");

        builder.HasKey(triggerEvent => triggerEvent.Id);
        builder.Property(triggerEvent => triggerEvent.Id)
            .HasColumnName("ID_EVENT");

        builder.Property(triggerEvent => triggerEvent.EventType)
            .HasColumnName("EVENT_TYPE")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(triggerEvent => triggerEvent.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(TriggerEvent.DescriptionMaxLength);

        builder.Property(triggerEvent => triggerEvent.Intensity)
            .HasColumnName("INTENSITY")
            .HasMaxLength(TriggerEvent.IntensityMaxLength);

        builder.Property(triggerEvent => triggerEvent.AlertSource)
            .HasColumnName("ALERT_SOURCE")
            .HasMaxLength(TriggerEvent.AlertSourceMaxLength)
            .IsRequired();

        builder.Property(triggerEvent => triggerEvent.StartDate)
            .HasColumnName("START_DATE");

        builder.Property(triggerEvent => triggerEvent.EndDate)
            .HasColumnName("END_DATE");

        builder.Property(triggerEvent => triggerEvent.Status)
            .HasColumnName("STATUS")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // IsActive é calculado a partir do Status; não vira coluna.
        builder.Ignore(triggerEvent => triggerEvent.IsActive);
    }
}
