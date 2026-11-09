using System.Data;
using backend.Application.Authentication;
using backend.Application.Interfaces.Authentication;
using backend.Domain.Users;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Data;

public sealed class RefreshSessionStore(IOptions<DatabaseOptions> options) : IRefreshSessionStore
{
    private readonly DatabaseOptions _options = options.Value;

    public async Task<bool> CreateAsync(
        int userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO [UserRefreshSessions] ([UserId], [TokenHash], [CreatedAt], [ExpiresAt])
            VALUES (@UserId, @TokenHash, @CreatedAt, @ExpiresAt);
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
        command.Parameters.Add(new SqlParameter("@TokenHash", SqlDbType.NVarChar, 128) { Value = tokenHash });
        command.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = createdAt.UtcDateTime });
        command.Parameters.Add(new SqlParameter("@ExpiresAt", SqlDbType.DateTime2) { Value = expiresAt.UtcDateTime });

        try
        {
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);
            return rows > 0;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    public async Task<RefreshSessionRotationResult> RotateAsync(
        string currentTokenHash,
        string replacementTokenHash,
        DateTimeOffset now,
        DateTimeOffset replacementExpiresAt,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string selectSql = """
            SELECT s.[Id], s.[UserId], s.[ExpiresAt], s.[RevokedAt], s.[ReplacedByTokenHash], u.[Status]
            FROM [UserRefreshSessions] s
            INNER JOIN [User] u ON s.[UserId] = u.[Id]
            WHERE s.[TokenHash] = @CurrentTokenHash;
            """;

        int sessionId;
        int userId;
        DateTime expiresAt;
        DateTime? revokedAt;
        string? replacedByTokenHash;
        string userStatusStr;

        await using (var selectCmd = new SqlCommand(selectSql, connection))
        {
            selectCmd.Parameters.Add(new SqlParameter("@CurrentTokenHash", SqlDbType.NVarChar, 128) { Value = currentTokenHash });
            await using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new RefreshSessionRotationResult(RefreshSessionRotationStatus.Invalid);
            }

            sessionId = reader.GetInt32(0);
            userId = reader.GetInt32(1);
            expiresAt = reader.GetDateTime(2);
            revokedAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
            replacedByTokenHash = reader.IsDBNull(4) ? null : reader.GetString(4);
            userStatusStr = reader.GetString(5);
        }

        if (revokedAt.HasValue)
        {
            return new RefreshSessionRotationResult(RefreshSessionRotationStatus.Revoked);
        }

        if (!string.IsNullOrEmpty(replacedByTokenHash))
        {
            return new RefreshSessionRotationResult(RefreshSessionRotationStatus.ReuseDetected);
        }

        if (new DateTimeOffset(expiresAt, TimeSpan.Zero) <= now)
        {
            return new RefreshSessionRotationResult(RefreshSessionRotationStatus.Expired);
        }

        if (!Enum.TryParse<UserState>(userStatusStr, ignoreCase: true, out var userState) ||
            userState is not (UserState.Active or UserState.Admin))
        {
            return new RefreshSessionRotationResult(RefreshSessionRotationStatus.AccountUnavailable);
        }

        // Rotate session in transaction
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        const string updateSql = """
            UPDATE [UserRefreshSessions]
            SET [RevokedAt] = @Now, [ReplacedByTokenHash] = @ReplacementTokenHash
            WHERE [Id] = @SessionId;
            """;

        await using (var updateCmd = new SqlCommand(updateSql, connection, transaction))
        {
            updateCmd.Parameters.Add(new SqlParameter("@Now", SqlDbType.DateTime2) { Value = now.UtcDateTime });
            updateCmd.Parameters.Add(new SqlParameter("@ReplacementTokenHash", SqlDbType.NVarChar, 128) { Value = replacementTokenHash });
            updateCmd.Parameters.Add(new SqlParameter("@SessionId", SqlDbType.Int) { Value = sessionId });
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        const string insertSql = """
            INSERT INTO [UserRefreshSessions] ([UserId], [TokenHash], [CreatedAt], [ExpiresAt])
            VALUES (@UserId, @ReplacementTokenHash, @Now, @ReplacementExpiresAt);
            """;

        await using (var insertCmd = new SqlCommand(insertSql, connection, transaction))
        {
            insertCmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
            insertCmd.Parameters.Add(new SqlParameter("@ReplacementTokenHash", SqlDbType.NVarChar, 128) { Value = replacementTokenHash });
            insertCmd.Parameters.Add(new SqlParameter("@Now", SqlDbType.DateTime2) { Value = now.UtcDateTime });
            insertCmd.Parameters.Add(new SqlParameter("@ReplacementExpiresAt", SqlDbType.DateTime2) { Value = replacementExpiresAt.UtcDateTime });
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return new RefreshSessionRotationResult(RefreshSessionRotationStatus.Rotated, userId, userState);
    }

    public async Task RevokeAsync(string tokenHash, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE [UserRefreshSessions]
            SET [RevokedAt] = @RevokedAt
            WHERE [TokenHash] = @TokenHash AND [RevokedAt] IS NULL;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@RevokedAt", SqlDbType.DateTime2) { Value = revokedAt.UtcDateTime });
        command.Parameters.Add(new SqlParameter("@TokenHash", SqlDbType.NVarChar, 128) { Value = tokenHash });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
