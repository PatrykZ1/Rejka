using System.Data;
using System.Text;
using backend.Application.Interfaces.Analysis;
using backend.Application.Interfaces.Media;
using backend.Domain.Locations;
using backend.Domain.Media;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MediaFile = backend.Domain.Media.File;

namespace backend.Infrastructure.Data;

public sealed class MediaRepository : IMediaRepository, IMediaAnalysisRepository
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
        bool enqueueImageAnalysis = false,
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

            if (enqueueImageAnalysis)
            {
                const string insertAnalysisJobSql = """
                    INSERT INTO [MediaAnalysisJobs] ([MediaId], [Status], [AttemptCount], [CreatedAt], [UpdatedAt])
                    VALUES (@MediaId, 'PENDING', 0, @CreatedAt, @CreatedAt);
                    """;
                await using var analysisJobCmd = new SqlCommand(insertAnalysisJobSql, connection, transaction);
                analysisJobCmd.Parameters.Add(new SqlParameter("@MediaId", SqlDbType.Int) { Value = mediaId });
                analysisJobCmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = occurredAt.UtcDateTime });
                await analysisJobCmd.ExecuteNonQueryAsync(cancellationToken);
                media.SetAnalysisStatus(MediaAnalysisStatus.Pending);
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
                   g.[latitude], g.[longitude], COALESCE(a.[Status], 'NOT_REQUESTED')
            FROM [Media] m
            LEFT JOIN [Geography] g ON m.[LocationId] = g.[Id]
            LEFT JOIN [MediaAnalysisJobs] a ON a.[MediaId] = m.[Id]
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

        var media = new FileImage(
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
        media.SetAnalysisStatus(ParseAnalysisStatus(reader.GetString(11)));

        return media;
    }

    public async Task<IReadOnlyList<MediaFile>> GetByUserAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT m.[Id], m.[UserId], m.[LocationId], m.[RecordedAt], m.[UploadedAt], m.[Comment], m.[Status], m.[MediaUrl], m.[FileName],
                   g.[latitude], g.[longitude], COALESCE(a.[Status], 'NOT_REQUESTED')
            FROM [Media] m
            LEFT JOIN [Geography] g ON m.[LocationId] = g.[Id]
            LEFT JOIN [MediaAnalysisJobs] a ON a.[MediaId] = m.[Id]
            WHERE m.[UserId] = @UserId AND m.[Status] <> 'DELETED'
            ORDER BY m.[UploadedAt] DESC, m.[Id] DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
        command.Parameters.Add(new SqlParameter("@Offset", SqlDbType.Int) { Value = (page - 1) * pageSize });
        command.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });

        var result = new List<MediaFile>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ReadMedia(reader));
        }

        return result;
    }

    public async Task<int> CountByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM [Media] WHERE [UserId] = @UserId AND [Status] <> 'DELETED';",
            connection);
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> GetRegistrationsByMediaIdsAsync(
        IReadOnlyCollection<int> mediaIds,
        CancellationToken cancellationToken = default)
    {
        var ids = mediaIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<int, IReadOnlyList<string>>();
        }

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var parameterNames = ids.Select((_, index) => $"@MediaId{index}").ToArray();
        var sql = $"""
            SELECT r.[MediaId], reg.[Number]
            FROM [Rating] r
            INNER JOIN [Registration] reg ON reg.[Id] = r.[RegistrationId]
            WHERE r.[MediaId] IN ({string.Join(", ", parameterNames)})
            ORDER BY r.[MediaId], reg.[Number];
            """;

        await using var command = new SqlCommand(sql, connection);
        for (var index = 0; index < ids.Length; index++)
        {
            command.Parameters.Add(new SqlParameter(parameterNames[index], SqlDbType.Int) { Value = ids[index] });
        }

        var registrations = new Dictionary<int, List<string>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var mediaId = reader.GetInt32(0);
            if (!registrations.TryGetValue(mediaId, out var list))
            {
                list = [];
                registrations.Add(mediaId, list);
            }

            list.Add(reader.GetString(1));
        }

        return registrations.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value);
    }

    public async Task<MediaAnalysisWorkItem?> ClaimNextAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE [MediaAnalysisJobs]
            SET [Status] = 'FAILED',
                [LastErrorCode] = 'RETRY_LIMIT',
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Status] = 'PROCESSING'
                AND [AttemptCount] >= @MaxAttempts
                AND [UpdatedAt] < DATEADD(MINUTE, -5, SYSUTCDATETIME());

            ;WITH nextJob AS (
                SELECT TOP (1) j.[MediaId]
                FROM [MediaAnalysisJobs] j WITH (UPDLOCK, READPAST, ROWLOCK)
                INNER JOIN [Media] m ON m.[Id] = j.[MediaId]
                WHERE m.[Status] <> 'DELETED'
                    AND m.[MediaUrl] IS NOT NULL
                    AND j.[AttemptCount] < @MaxAttempts
                    AND (
                        (j.[Status] = 'PENDING'
                            AND (j.[AttemptCount] = 0 OR j.[UpdatedAt] < DATEADD(SECOND, 5, SYSUTCDATETIME())))
                        OR (j.[Status] = 'PROCESSING' AND j.[UpdatedAt] < DATEADD(MINUTE, -5, SYSUTCDATETIME()))
                    )
                ORDER BY j.[CreatedAt], j.[MediaId]
            )
            UPDATE j
            SET [Status] = 'PROCESSING',
                [AttemptCount] = [AttemptCount] + 1,
                [UpdatedAt] = SYSUTCDATETIME(),
                [LastErrorCode] = NULL
            OUTPUT inserted.[MediaId], m.[MediaUrl], inserted.[AttemptCount]
            FROM [MediaAnalysisJobs] j
            INNER JOIN nextJob n ON n.[MediaId] = j.[MediaId]
            INNER JOIN [Media] m ON m.[Id] = j.[MediaId];
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@MaxAttempts", SqlDbType.Int) { Value = 5 });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MediaAnalysisWorkItem(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2));
    }

    public async Task CompleteAsync(
        int mediaId,
        IReadOnlyCollection<PlateRecognition> plates,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var mediaCommand = new SqlCommand(
                "SELECT [Status] FROM [Media] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = @MediaId;",
                connection,
                transaction);
            mediaCommand.Parameters.Add(new SqlParameter("@MediaId", SqlDbType.Int) { Value = mediaId });
            var mediaStatus = await mediaCommand.ExecuteScalarAsync(cancellationToken) as string;
            if (mediaStatus is null)
            {
                throw new InvalidOperationException($"Media {mediaId} no longer exists.");
            }

            if (!string.Equals(mediaStatus, "DELETED", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var registration in plates
                    .Select(plate => plate.Registration.Trim().ToUpperInvariant())
                    .Distinct(StringComparer.Ordinal))
                {
                    if (registration.Length is < 4 or > 16
                        || registration.Any(character => !char.IsAsciiLetterOrDigit(character)))
                    {
                        throw new InvalidOperationException("The recognizer returned an invalid registration.");
                    }

                    await using var registrationCommand = new SqlCommand(
                        """
                        SELECT [Id]
                        FROM [Registration] WITH (UPDLOCK, HOLDLOCK)
                        WHERE [Number] = @Number;
                        """,
                        connection,
                        transaction);
                    registrationCommand.Parameters.Add(new SqlParameter("@Number", SqlDbType.NVarChar, 16) { Value = registration });
                    var registrationIdValue = await registrationCommand.ExecuteScalarAsync(cancellationToken);
                    int registrationId;
                    if (registrationIdValue is int existingId)
                    {
                        registrationId = existingId;
                    }
                    else
                    {
                        await using var insertRegistrationCommand = new SqlCommand(
                            """
                            INSERT INTO [Registration] ([Number])
                            VALUES (@Number);
                            SELECT CAST(SCOPE_IDENTITY() AS INT);
                            """,
                            connection,
                            transaction);
                        insertRegistrationCommand.Parameters.Add(new SqlParameter("@Number", SqlDbType.NVarChar, 16) { Value = registration });
                        registrationId = Convert.ToInt32(await insertRegistrationCommand.ExecuteScalarAsync(cancellationToken));
                    }

                    await using var relationCommand = new SqlCommand(
                        """
                        IF NOT EXISTS (
                            SELECT 1 FROM [Rating] WITH (UPDLOCK, HOLDLOCK)
                            WHERE [RegistrationId] = @RegistrationId AND [MediaId] = @MediaId
                        )
                        BEGIN
                            INSERT INTO [Rating] ([RegistrationId], [MediaId], [Rating])
                            VALUES (@RegistrationId, @MediaId, NULL);
                        END;
                        """,
                        connection,
                        transaction);
                    relationCommand.Parameters.Add(new SqlParameter("@RegistrationId", SqlDbType.Int) { Value = registrationId });
                    relationCommand.Parameters.Add(new SqlParameter("@MediaId", SqlDbType.Int) { Value = mediaId });
                    await relationCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await using var completeCommand = new SqlCommand(
                """
                UPDATE [MediaAnalysisJobs]
                SET [Status] = 'COMPLETED',
                    [UpdatedAt] = SYSUTCDATETIME(),
                    [LastErrorCode] = NULL
                WHERE [MediaId] = @MediaId;
                """,
                connection,
                transaction);
            completeCommand.Parameters.Add(new SqlParameter("@MediaId", SqlDbType.Int) { Value = mediaId });
            if (await completeCommand.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                throw new InvalidOperationException($"Media analysis job {mediaId} no longer exists.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task FailAsync(
        int mediaId,
        string errorCode,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE [MediaAnalysisJobs]
            SET [Status] = CASE WHEN [AttemptCount] >= @MaxAttempts THEN 'FAILED' ELSE 'PENDING' END,
                [LastErrorCode] = @ErrorCode,
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [MediaId] = @MediaId AND [Status] = 'PROCESSING';
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@MaxAttempts", SqlDbType.Int) { Value = 5 });
        command.Parameters.Add(new SqlParameter("@ErrorCode", SqlDbType.NVarChar, 64) { Value = errorCode });
        command.Parameters.Add(new SqlParameter("@MediaId", SqlDbType.Int) { Value = mediaId });
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException($"Media analysis job {mediaId} is not processing.");
        }
    }

    public async Task<bool> UpdateMetadataAsync(
        int mediaId,
        int userId,
        string? fileName,
        string? comment,
        DateTimeOffset? recordedAt,
        decimal? latitude,
        decimal? longitude,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            int? locationId = null;
            if (latitude.HasValue && longitude.HasValue)
            {
                const string insertGeoSql = """
                    INSERT INTO [Geography] ([latitude], [longitude])
                    VALUES (@Latitude, @Longitude);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                    """;

                await using var geoCmd = new SqlCommand(insertGeoSql, connection, transaction);
                geoCmd.Parameters.Add(new SqlParameter("@Latitude", SqlDbType.Decimal) { Precision = 8, Scale = 6, Value = latitude.Value });
                geoCmd.Parameters.Add(new SqlParameter("@Longitude", SqlDbType.Decimal) { Precision = 9, Scale = 6, Value = longitude.Value });
                locationId = Convert.ToInt32(await geoCmd.ExecuteScalarAsync(cancellationToken));
            }

            const string sql = """
                UPDATE [Media]
                SET [FileName] = COALESCE(@FileName, [FileName]),
                    [Comment] = @Comment,
                    [RecordedAt] = @RecordedAt,
                    [LocationId] = @LocationId
                WHERE [Id] = @MediaId AND [UserId] = @UserId AND [Status] <> 'DELETED';
                """;

            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar, 255) { Value = (object?)fileName?.Trim() ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@Comment", SqlDbType.NVarChar) { Value = (object?)comment ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@RecordedAt", SqlDbType.DateTime2) { Value = (object?)recordedAt?.UtcDateTime ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@LocationId", SqlDbType.Int) { Value = (object?)locationId ?? DBNull.Value });
            command.Parameters.Add(new SqlParameter("@MediaId", SqlDbType.Int) { Value = mediaId });
            command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return affected > 0;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        int mediaId,
        int userId,
        DateTimeOffset deletedAt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE [Media]
            SET [Status] = 'DELETED'
            WHERE [Id] = @MediaId AND [UserId] = @UserId AND [Status] <> 'DELETED';

            IF @@ROWCOUNT > 0
            BEGIN
                INSERT INTO [MediaActivityRecords] ([MediaId], [Action], [OccurredAt])
                VALUES (@MediaId, 'DELETE', @DeletedAt);
            END;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@MediaId", SqlDbType.Int) { Value = mediaId });
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
        command.Parameters.Add(new SqlParameter("@DeletedAt", SqlDbType.DateTime2) { Value = deletedAt.UtcDateTime });
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static MediaFile ReadMedia(SqlDataReader reader)
    {
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

        var media = new FileImage(
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
        var analysisStatusText = reader.GetString(11);
        media.SetAnalysisStatus(ParseAnalysisStatus(analysisStatusText));

        return media;
    }

    private static MediaAnalysisStatus ParseAnalysisStatus(string value)
    {
        if (string.Equals(value, "NOT_REQUESTED", StringComparison.OrdinalIgnoreCase))
        {
            return MediaAnalysisStatus.NotRequested;
        }

        if (Enum.TryParse<MediaAnalysisStatus>(value, ignoreCase: true, out var status))
        {
            return status;
        }

        throw new InvalidOperationException($"Unknown media analysis status '{value}'.");
    }

}
