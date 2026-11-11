using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaCuida.Infrastructure.Idempotency;

namespace SampaCuida.Infrastructure.Persistence.Configurations;

// Tabela técnica (fora do DER), no mesmo padrão de nomes das demais.
public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IDEMPOTENCY_RECORD");

        // A própria chave é a chave primária: duas requisições com a mesma chave
        // ao mesmo tempo não conseguem as duas reservá-la. Quem garante é o banco.
        builder.HasKey(record => record.Key);
        builder.Property(record => record.Key)
            .HasColumnName("IDEMPOTENCY_KEY")
            .HasMaxLength(IdempotencyRecord.KeyMaxLength);

        builder.Property(record => record.RequestHash)
            .HasColumnName("REQUEST_HASH")
            .HasMaxLength(IdempotencyRecord.RequestHashLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(record => record.StatusCode)
            .HasColumnName("STATUS_CODE");

        builder.Property(record => record.ResponseBody)
            .HasColumnName("RESPONSE_BODY");

        builder.Property(record => record.CreatedAt)
            .HasColumnName("CREATED_AT");
    }
}
