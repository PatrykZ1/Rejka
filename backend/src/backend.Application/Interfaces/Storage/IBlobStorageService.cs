namespace backend.Application.Interfaces.Storage;

public interface IBlobStorageService
{
    Task<string> UploadBlobAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadBlobAsync(string blobName, CancellationToken cancellationToken = default);
    string GenerateSasUri(string blobName, TimeSpan validFor);
    Task DeleteBlobAsync(string blobName, CancellationToken cancellationToken = default);
}
