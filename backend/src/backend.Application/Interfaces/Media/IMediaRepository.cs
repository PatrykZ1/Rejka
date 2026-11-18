using MediaFile = backend.Domain.Media.File;

namespace backend.Application.Interfaces.Media;

public interface IMediaRepository
{
    Task<int> AddWithActivityRecordAsync(
        MediaFile media,
        string action,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default);
    Task<MediaFile?> GetByIdAsync(int mediaId, CancellationToken cancellationToken = default);
}
