using System.Data;
using backend.Application.Interfaces.Media;
using backend.Domain.Locations;
using backend.Domain.Media;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MediaFile = backend.Domain.Media.File;

namespace backend.Infrastructure.Data;

public sealed class MediaRepository : IMediaRepository
{
    private readonly DatabaseOptions _options;

    public MediaRepository(IOptions<DatabaseOptions> options)
    {
        _options = options.Value;
    }

    public async Task<int> AddWithActivityRecordAsync(
        MediaFile media,
        string action,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(media);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            int? locationId = null;

            if (media.Location is not null)
            {
                const string insertGeoSql = """
                    INSERT INTO [Geography] ([latitude], [longitude])
                    VALUES (@Latitude, @Longitude);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                    """;

                await using var geoCmd = new SqlCommand(insertGeoSql, connection, transaction);
                geoCmd.Parameters.Add(new SqlParameter("@Latitude", SqlDbType.Decimal) { Precision = 8, Scale = 6, Value = media.Location.Latitude });
                geoCmd.Parameters.Add(new SqlParameter("@Longitude", SqlDbType.Decimal) { Precision = 9, Scale = 6, Value = media.Location.Longitude });

                var geoIdObj = await geoCmd.ExecuteScalarAsync(cancellationToken);
                if (geoIdObj is int id)
                {
                    locationId = id;
                    media.Location.SetId(id);
                }
            }

            const string insertMediaSql = """
                INSERT INTO [Media] ([UserId], [LocationId], [RecordedAt], [UploadedAt], [Comment], [Status], [MediaUrl], [FileName])
                VALUES (@UserId, @LocationId, @RecordedAt, @UploadedAt, @Comment, @Status, @MediaUrl, @FileName);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

            await using var mediaCmd = new SqlCommand(insertMediaSql, connection, transaction);
            mediaCmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = media.UserId });
            mediaCmd.Parameters.Add(new SqlParameter("@LocationId", SqlDbType.Int) { Value = (object?)locationId ?? DBNull.Value });
            mediaCmd.Parameters.Add(new SqlParameter("@RecordedAt", SqlDbType.DateTime2) { Value = (object?)media.RecordedAt?.UtcDateTime ?? DBNull.Value });
            mediaCmd.Parameters.Add(new SqlParameter("@UploadedAt", SqlDbType.DateTime2) { Value = media.UploadedAt.UtcDateTime });
            mediaCmd.Parameters.Add(new SqlParameter("@Comment", SqlDbType.NVarChar) { Value = (object?)media.Comment ?? DBNull.Value });
            mediaCmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 32) { Value = media.State.ToString().ToUpperInvariant() });
            mediaCmd.Parameters.Add(new SqlParameter("@MediaUrl", SqlDbType.NVarChar, 2048) { Value = (object?)media.MediaUrl ?? DBNull.Value });
            mediaCmd.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar, 255) { Value = media.FileName });

            var mediaIdObj = await mediaCmd.ExecuteScalarAsync(cancellationToken);
            if (mediaIdObj is not int mediaId)
            {
                throw new InvalidOperationException("Nie udało się pobrać identyfikatora utworzonego rekordu Media.");
            }

            const string insertActivitySql = """
                INSERT INTO [MediaActivityRecords] ([MediaId], [Action], [OccurredAt])
                VALUES (@MediaId, @Action, @OccurredAt);
                """;

            await using var activityCmd = new SqlCommand(insertActivitySql, connection, transaction);
            activityCmd.Parameters.Add(new SqlParameter("@MediaId", SqlDbType.Int) { Value = mediaId });
            activityCmd.Parameters.Add(new SqlParameter("@Action", SqlDbType.NVarChar, 64) { Value = action });
            activityCmd.Parameters.Add(new SqlParameter("@OccurredAt", SqlDbType.DateTime2) { Value = occurredAt.UtcDateTime });
            await activityCmd.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return mediaId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<MediaFile?> GetByIdAsync(int mediaId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT m.[Id], m.[UserId], m.[LocationId], m.[RecordedAt], m.[UploadedAt], m.[Comment], m.[Status], m.[MediaUrl], m.[FileName],
                   g.[latitude], g.[longitude]
            FROM [Media] m
            LEFT JOIN [Geography] g ON m.[LocationId] = g.[Id]
            WHERE m.[Id] = @MediaId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@MediaId", SqlDbType.Int) { Value = mediaId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var id = reader.GetInt32(0);
        var userId = reader.GetInt32(1);
        int? locationId = reader.IsDBNull(2) ? null : reader.GetInt32(2);
        DateTimeOffset? recordedAt = reader.IsDBNull(3) ? null : new DateTimeOffset(reader.GetDateTime(3), TimeSpan.Zero);
        var uploadedAt = new DateTimeOffset(reader.GetDateTime(4), TimeSpan.Zero);
        string? comment = reader.IsDBNull(5) ? null : reader.GetString(5);
        var statusStr = reader.GetString(6);
        string? mediaUrl = reader.IsDBNull(7) ? null : reader.GetString(7);
        var fileName = reader.GetString(8);

        Location? location = null;
        if (locationId.HasValue && !reader.IsDBNull(9) && !reader.IsDBNull(10))
        {
            location = new Location(locationId.Value, reader.GetDecimal(9), reader.GetDecimal(10));
        }

        if (!Enum.TryParse<FileState>(statusStr, ignoreCase: true, out var fileState))
        {
            fileState = FileState.Active;
        }

        return new FileImage(
            id: id,
            fileName: fileName,
            userId: userId,
            mediaUrl: mediaUrl,
            uploadedAt: uploadedAt,
            recordedAt: recordedAt,
            comment: comment,
            state: fileState,
            locationId: locationId,
            location: location);
    }

}
