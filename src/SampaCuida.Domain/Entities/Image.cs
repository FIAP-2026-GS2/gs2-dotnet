using SampaCuida.Domain.Common;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.Domain.Entities;

/// <summary>
/// Arquivo de uma captura guardado no Blob Storage. A API guarda só a URL, nunca o arquivo.
/// Toda imagem gravada foi anonimizada (rostos e placas borrados) antes de chegar aqui.
/// </summary>
public class Image
{
    public const int FileNameMaxLength = 255;
    public const int FilePathMaxLength = 500;
    public const int FileHashLength = 64;

    public int Id { get; private set; }
    public int CaptureId { get; private set; }
    public Capture? Capture { get; private set; }
    public string FileName { get; private set; } = null!;
    public string FilePath { get; private set; } = null!;
    public string? FileHash { get; private set; }
    public DateTime StorageDate { get; private set; }

    // Usado apenas pelo EF Core ao materializar registros do banco.
    private Image() { }

    // Para uma captura que já existe no banco.
    public Image(
        int captureId,
        string fileName,
        string filePath,
        bool isAnonymized,
        DateTime storageDate,
        string? fileHash = null)
    {
        CaptureId = Guard.RequiredId(captureId, "captura");
        SetFile(fileName, filePath, isAnonymized, storageDate, fileHash);
    }

    // Para uma captura nova: o EF grava as duas juntas e preenche o id da captura.
    public Image(
        Capture capture,
        string fileName,
        string filePath,
        bool isAnonymized,
        DateTime storageDate,
        string? fileHash = null)
    {
        Capture = capture ?? throw new DomainException("A captura é obrigatória.");
        SetFile(fileName, filePath, isAnonymized, storageDate, fileHash);
    }

    private void SetFile(string fileName, string filePath, bool isAnonymized, DateTime storageDate, string? fileHash)
    {
        // Regra de privacidade: imagem sem anonimização nunca é armazenada.
        if (!isAnonymized)
            throw new DomainException("A imagem precisa estar anonimizada (rostos e placas borrados).");

        FileName = Guard.RequiredText(fileName, "nome do arquivo", FileNameMaxLength);
        FilePath = HttpsUrl(filePath);
        StorageDate = Guard.Utc(storageDate, "armazenamento");
        FileHash = Sha256(fileHash);
    }

    private static string HttpsUrl(string filePath)
    {
        var value = Guard.RequiredText(filePath, "caminho do arquivo", FilePathMaxLength);

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new DomainException("O caminho do arquivo deve ser uma URL https.");

        return value;
    }

    // Impressão digital do arquivo (SHA-256 em hexadecimal), opcional.
    private static string? Sha256(string? fileHash)
    {
        var value = Guard.OptionalText(fileHash, "hash do arquivo", FileHashLength)?.ToLowerInvariant();

        if (value is not null && (value.Length != FileHashLength || !value.All(char.IsAsciiHexDigitLower)))
            throw new DomainException("O hash do arquivo deve ser um SHA-256 com 64 caracteres hexadecimais.");

        return value;
    }
}
