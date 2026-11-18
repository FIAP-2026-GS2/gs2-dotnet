using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Photos;

namespace SampaTech.Infrastructure.Photos;

/// <summary>
/// Guarda as fotos dos cidadãos no Blob Storage (no local, o emulador Azurite do docker-compose).
/// O container é privado e é criado na primeira foto. Falha de armazenamento vira 503.
/// </summary>
public class BlobPhotoStorage(
    IConfiguration configuration,
    IOptions<CitizenPhotoOptions> options,
    ILogger<BlobPhotoStorage> logger) : IPhotoStorage
{
    public async Task<string> SaveAsync(
        string fileName, string contentType, byte[] content, CancellationToken cancellationToken)
    {
        if (configuration.GetConnectionString("BlobStorage") is not { Length: > 0 } connectionString)
            throw new ServiceUnavailableException(
                "O armazenamento de fotos não está configurado (ConnectionStrings:BlobStorage). Nada foi guardado.");

        try
        {
            var container = new BlobContainerClient(connectionString, options.Value.ContainerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            var blob = container.GetBlobClient(fileName);
            await blob.UploadAsync(new BinaryData(content),
                new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
                cancellationToken);

            return blob.Uri.ToString();
        }
        catch (Exception exception) when (exception is RequestFailedException or AggregateException
                                              or HttpRequestException)
        {
            logger.LogWarning(exception, "Falha ao guardar a foto no Blob Storage.");
            throw new ServiceUnavailableException(
                "O armazenamento de fotos não está disponível agora. Nada foi guardado; tente de novo em instantes.");
        }
    }

    public async Task<StoredPhoto?> ReadAsync(string fileName, CancellationToken cancellationToken)
    {
        if (configuration.GetConnectionString("BlobStorage") is not { Length: > 0 } connectionString)
            throw new ServiceUnavailableException("O armazenamento de fotos não está configurado (ConnectionStrings:BlobStorage).");

        try
        {
            var blob = new BlobContainerClient(connectionString, options.Value.ContainerName).GetBlobClient(fileName);
            var download = await blob.DownloadContentAsync(cancellationToken);
            return new StoredPhoto(download.Value.Content.ToArray(),
                download.Value.Details.ContentType ?? PhotoFormat.Jpeg);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
        catch (Exception exception) when (exception is RequestFailedException or HttpRequestException)
        {
            logger.LogWarning(exception, "Falha ao ler a foto no Blob Storage.");
            throw new ServiceUnavailableException("O armazenamento de fotos não está disponível agora. Tente de novo em instantes.");
        }
    }
}
