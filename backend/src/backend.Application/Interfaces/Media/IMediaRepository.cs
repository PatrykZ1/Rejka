using MediaFile = backend.Domain.Media.File;

namespace backend.Application.Interfaces.Media;

public interface IMediaRepository
{
    Task<int> AddWithActivityRecordAsync(
        MediaFile media,
        string action,
        DateTimeOffset occurredAt,
        bool enqueueMediaAnalysis = false,
        CancellationToken cancellationToken = default);
    Task<MediaFile?> GetByIdAsync(int mediaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaFile>> GetByUserAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMetadataAsync(
        int mediaId,
        int userId,
        string? fileName,
        string? comment,
        DateTimeOffset? recordedAt,
        decimal? latitude,
        decimal? longitude,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(
        int mediaId,
        int userId,
        DateTimeOffset deletedAt,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> GetRegistrationsByMediaIdsAsync(
        IReadOnlyCollection<int> mediaIds,
        CancellationToken cancellationToken = default);
}
