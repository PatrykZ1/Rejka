using backend.Domain.Locations;

namespace backend.Domain.Media;

public sealed class FileImage : File
{
    public FileImage(
        string fileName,
        int userId,
        string? mediaUrl,
        DateTimeOffset uploadedAt,
        DateTimeOffset? recordedAt = null,
        string? comment = null,
        int? locationId = null,
        Location? location = null)
        : base(fileName, userId, mediaUrl, uploadedAt, recordedAt, comment, locationId, location)
    {
    }

    public FileImage(
        int id,
        string fileName,
        int userId,
        string? mediaUrl,
        DateTimeOffset uploadedAt,
        DateTimeOffset? recordedAt,
        string? comment,
        FileState state,
        int? locationId,
        Location? location)
        : base(id, fileName, userId, mediaUrl, uploadedAt, recordedAt, comment, state, locationId, location)
    {
    }
}
