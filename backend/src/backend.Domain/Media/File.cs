using backend.Domain.Locations;

namespace backend.Domain.Media;

public abstract class File
{
    public int Id { get; protected set; }
    public string FileName { get; protected set; }
    public int UserId { get; protected set; }
    public int? LocationId { get; protected set; }
    public Location? Location { get; protected set; }
    public string? Comment { get; protected set; }
    public DateTimeOffset? RecordedAt { get; protected set; }
    public DateTimeOffset UploadedAt { get; protected set; }
    public FileState State { get; protected set; }
    public string? MediaUrl { get; protected set; }

    protected File(
        string fileName,
        int userId,
        string? mediaUrl,
        DateTimeOffset uploadedAt,
        DateTimeOffset? recordedAt = null,
        string? comment = null,
        int? locationId = null,
        Location? location = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("FileName is required.", nameof(fileName));
        }

        FileName = fileName.Trim();
        UserId = userId;
        MediaUrl = mediaUrl;
        UploadedAt = uploadedAt;
        RecordedAt = recordedAt;
        Comment = comment;
        LocationId = locationId;
        Location = location;
        State = FileState.Active;
    }

    protected File(
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
    {
        Id = id;
        FileName = fileName;
        UserId = userId;
        MediaUrl = mediaUrl;
        UploadedAt = uploadedAt;
        RecordedAt = recordedAt;
        Comment = comment;
        State = state;
        LocationId = locationId;
        Location = location;
    }

    public void SetId(int id)
    {
        Id = id;
    }

    public void SetMediaUrl(string mediaUrl)
    {
        MediaUrl = mediaUrl;
    }

    public void UpdateMetadata(
        string? fileName,
        string? comment,
        DateTimeOffset? recordedAt,
        int? locationId,
        Location? location)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            FileName = fileName.Trim();
        }

        Comment = comment;
        RecordedAt = recordedAt;
        LocationId = locationId;
        Location = location;
    }

    public bool ChangeState(FileState newState)
    {
        State = newState;
        return true;
    }
}
