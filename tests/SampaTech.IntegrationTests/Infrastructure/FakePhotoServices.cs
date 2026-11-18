using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Photos;

namespace SampaTech.IntegrationTests.Infrastructure;

/// <summary>
/// IA e Blob de mentira, controláveis pelo teste. A "IA" devolve a mesma imagem (como se tivesse
/// borrado) ou finge estar fora do ar; o "Blob" só devolve uma URL e guarda o que recebeu.
/// </summary>
public class FakePhotoServices : IPhotoAnonymizer, IPhotoStorage
{
    public bool AiAvailable { get; set; } = true;
    public List<string> StoredFiles { get; } = [];
    private readonly Dictionary<string, StoredPhoto> _files = [];

    public void Reset()
    {
        AiAvailable = true;
        StoredFiles.Clear();
        _files.Clear();
    }

    public Task<byte[]> AnonymizeAsync(PhotoUpload photo, string contentType, CancellationToken cancellationToken) =>
        AiAvailable
            ? Task.FromResult(photo.Content)
            : throw new ServiceUnavailableException("O serviço de IA não conseguiu borrar a foto agora. Nada foi guardado.");

    public Task<string> SaveAsync(string fileName, string contentType, byte[] content, CancellationToken cancellationToken)
    {
        StoredFiles.Add(fileName);
        _files[fileName] = new StoredPhoto(content, contentType);
        return Task.FromResult($"https://teste.blob.core.windows.net/citizen-photos/{fileName}");
    }

    public Task<StoredPhoto?> ReadAsync(string fileName, CancellationToken cancellationToken) =>
        Task.FromResult(_files.GetValueOrDefault(fileName));
}
