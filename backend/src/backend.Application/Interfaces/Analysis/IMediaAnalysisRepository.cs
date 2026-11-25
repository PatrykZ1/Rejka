namespace backend.Application.Interfaces.Analysis;

public sealed record MediaAnalysisWorkItem(int MediaId, string BlobName, int AttemptCount);

public interface IMediaAnalysisRepository
{
    Task<MediaAnalysisWorkItem?> ClaimNextAsync(CancellationToken cancellationToken = default);
    Task CompleteAsync(
        int mediaId,
        IReadOnlyCollection<PlateRecognition> plates,
        CancellationToken cancellationToken = default);
    Task FailAsync(int mediaId, string errorCode, CancellationToken cancellationToken = default);
}
