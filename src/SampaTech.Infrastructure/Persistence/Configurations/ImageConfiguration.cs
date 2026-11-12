using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampaTech.Domain.Entities;

namespace SampaTech.Infrastructure.Persistence.Configurations;

// Nomes de tabela e colunas iguais ao modelo relacional (DER) do grupo.
public class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder.ToTable("IMAGE");

        builder.HasKey(image => image.Id);
        builder.Property(image => image.Id)
            .HasColumnName("ID_IMAGE");

        builder.Property(image => image.FileName)
            .HasColumnName("FILE_NAME")
            .HasMaxLength(Image.FileNameMaxLength)
            .IsRequired();

        builder.Property(image => image.FilePath)
            .HasColumnName("FILE_PATH")
            .HasMaxLength(Image.FilePathMaxLength)
            .IsRequired();

        builder.Property(image => image.FileHash)
            .HasColumnName("FILE_HASH")
            .HasMaxLength(Image.FileHashLength)
            .IsFixedLength();

        builder.Property(image => image.StorageDate)
            .HasColumnName("STORAGE_DATE");

        builder.Property(image => image.CaptureId)
            .HasColumnName("ID_CAPTURE");

        builder.HasOne(image => image.Capture)
            .WithMany()
            .HasForeignKey(image => image.CaptureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
