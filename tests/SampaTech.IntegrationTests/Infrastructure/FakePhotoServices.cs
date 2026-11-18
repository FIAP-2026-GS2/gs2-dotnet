using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Photos;
using SampaTech.Domain.Enums;

namespace SampaTech.IntegrationTests.Infrastructure;

/// <summary>
/// IA e Blob de mentira, controláveis pelo teste. A "IA" devolve a mesma imagem (como se tivesse
/// borrado), recusa a foto com um motivo ou finge estar fora do ar; o "Blob" só devolve uma URL e
/// guarda o que recebeu.
/// </summary>
public class FakePhotoServices : IPhotoVerifier, IPhotoStorage
{
    public bool AiAvailable { get; set; } = true;

    // Preenchido: a "IA" recusa a foto com este motivo.
    public string? RejectionReason { get; set; }

    // O que a .NET mandou para a "IA" na última conferência.
    public (IssueCategory Category, CitizenFeedbackDecision Decision)? LastCheck { get; private set; }

    public List<string> StoredFiles { get; } = [];
    private readonly Dictionary<string, StoredPhoto> _files = [];

    public void Reset()
    {
        AiAvailable = true;
        RejectionReason = null;
        LastCheck = null;
        StoredFiles.Clear();
        _files.Clear();
    }

    public Task<VerifiedPhoto> VerifyAsync(
        PhotoUpload photo, string contentType, IssueCategory category, CitizenFeedbackDecision decision,
        CancellationToken cancellationToken)
    {
        if (!AiAvailable)
            throw new ServiceUnavailableException("O serviço de IA não conseguiu conferir a foto agora. Nada foi guardado.");

        LastCheck = (category, decision);
        return Task.FromResult(RejectionReason is null
            ? new VerifiedPhoto(true, "Foto conferida pela IA.", photo.Content)
            : new VerifiedPhoto(false, RejectionReason, []));
    }

    public Task<string> SaveAsync(string fileName, string contentType, byte[] content, CancellationToken cancellationToken)
    {
        StoredFiles.Add(fileName);
        _files[fileName] = new StoredPhoto(content, contentType);
        return Task.FromResult($"https://teste.blob.core.windows.net/citizen-photos/{fileName}");
    }

    public Task<StoredPhoto?> ReadAsync(string fileName, CancellationToken cancellationToken) =>
        Task.FromResult(_files.GetValueOrDefault(fileName));
}
