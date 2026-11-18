namespace SampaTech.Application.Common.Photos;

/// <summary>Foto recebida do cidadão, já lida para a memória (o tamanho é limitado).</summary>
public record PhotoUpload(string FileName, byte[] Content);

/// <summary>
/// Borra rostos e placas. A implementação chama o serviço de IA (Infrastructure); se ele estiver
/// fora do ar, lança ServiceUnavailableException e nada é guardado.
/// </summary>
public interface IPhotoAnonymizer
{
    Task<byte[]> AnonymizeAsync(PhotoUpload photo, string contentType, CancellationToken cancellationToken);
}

/// <summary>Arquivo lido do armazenamento, pronto para devolver na resposta.</summary>
public record StoredPhoto(byte[] Content, string ContentType);

/// <summary>Guarda o arquivo (Blob Storage) e devolve a URL dele; lê de volta pelo nome.</summary>
public interface IPhotoStorage
{
    Task<string> SaveAsync(string fileName, string contentType, byte[] content, CancellationToken cancellationToken);

    // Nulo quando o arquivo não existe mais no armazenamento.
    Task<StoredPhoto?> ReadAsync(string fileName, CancellationToken cancellationToken);
}

/// <summary>Configuração "CitizenPhotos" do appsettings.</summary>
public class CitizenPhotoOptions
{
    public const string SectionName = "CitizenPhotos";

    public int MaxBytes { get; set; } = 5 * 1024 * 1024;
    public string ContainerName { get; set; } = "citizen-photos";
}

/// <summary>
/// Descobre o formato pelos primeiros bytes do arquivo ("assinatura"), e não pela extensão ou pelo
/// Content-Type, que o cliente pode mandar errado. Aceita só JPEG e PNG.
/// </summary>
public static class PhotoFormat
{
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static string? Detect(byte[] content) =>
        content.AsSpan().StartsWith(JpegSignature) ? Jpeg
        : content.AsSpan().StartsWith(PngSignature) ? Png
        : null;

    public static string Extension(string contentType) => contentType == Png ? "png" : "jpg";
}
