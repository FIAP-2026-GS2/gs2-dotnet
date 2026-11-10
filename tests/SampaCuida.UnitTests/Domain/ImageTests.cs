using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Exceptions;

namespace SampaCuida.UnitTests.Domain;

public class ImageTests
{
    private const string Url = "https://sampacuida.blob.core.windows.net/frames/cam-001/2026-11-10T15-00.jpg";
    private static readonly DateTime Stored = new(2026, 11, 10, 15, 1, 0, DateTimeKind.Utc);
    private static readonly string ValidHash = new('a', 64);

    private static Image CreateImage(
        string filePath = Url,
        bool isAnonymized = true,
        string? fileHash = null) =>
        new(captureId: 1, "2026-11-10T15-00.jpg", filePath, isAnonymized, Stored, fileHash);

    [Fact]
    public void Constructor_with_valid_data_creates_image()
    {
        var image = CreateImage(fileHash: ValidHash);

        Assert.Equal(Url, image.FilePath);
        Assert.Equal(ValidHash, image.FileHash);
        Assert.Equal(Stored, image.StorageDate);
    }

    [Fact]
    public void Constructor_rejects_image_that_is_not_anonymized()
    {
        var exception = Assert.Throws<DomainException>(() => CreateImage(isAnonymized: false));

        Assert.Contains("anonimizada", exception.Message);
    }

    [Theory]
    [InlineData("http://sampacuida.blob.core.windows.net/frames/a.jpg")]
    [InlineData("frames/a.jpg")]
    [InlineData("C:\\imagens\\a.jpg")]
    public void Constructor_rejects_path_that_is_not_https_url(string filePath)
    {
        var exception = Assert.Throws<DomainException>(() => CreateImage(filePath: filePath));

        Assert.Contains("https", exception.Message);
    }

    [Fact]
    public void Constructor_normalizes_hash_to_lowercase()
    {
        var image = CreateImage(fileHash: new string('A', 64));

        Assert.Equal(ValidHash, image.FileHash);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void Constructor_rejects_malformed_hash(string fileHash)
    {
        Assert.Throws<DomainException>(() => CreateImage(fileHash: fileHash));
    }

    [Fact]
    public void Constructor_accepts_image_without_hash()
    {
        var image = CreateImage(fileHash: "  ");

        Assert.Null(image.FileHash);
    }
}
