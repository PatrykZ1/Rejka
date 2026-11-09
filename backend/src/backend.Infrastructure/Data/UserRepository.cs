using System.Data;
using backend.Application.Interfaces.Users;
using backend.Domain.Users;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Data;

public sealed class UserRepository(IOptions<DatabaseOptions> options) : IUserRepository
{
    private readonly DatabaseOptions _options = options.Value;

    public async Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return null;
        }

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT TOP(1) [Id], [Username], [Email], [Status], [PasswordHash], [CreatedAt], [DeletedAt]
            FROM [User]
            WHERE UPPER([Email]) = @NormalizedEmail;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@NormalizedEmail", SqlDbType.NVarChar, 255) { Value = normalizedEmail.Trim().ToUpperInvariant() });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var id = reader.GetInt32(0);
        var username = reader.GetString(1);
        var email = reader.GetString(2);
        var statusStr = reader.GetString(3);
        var passwordHash = reader.GetString(4);
        var createdAt = new DateTimeOffset(reader.GetDateTime(5), TimeSpan.Zero);
        DateTimeOffset? deletedAt = reader.IsDBNull(6) ? null : new DateTimeOffset(reader.GetDateTime(6), TimeSpan.Zero);

        if (!Enum.TryParse<UserState>(statusStr, ignoreCase: true, out var userState))
        {
            userState = UserState.Active;
        }

        return new User(
            id,
            username,
            email,
            email.ToUpperInvariant(),
            passwordHash,
            userState,
            createdAt,
            deletedAt);
    }

    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO [User] ([Username], [Email], [Status], [PasswordHash], [CreatedAt], [DeletedAt])
            VALUES (@Username, @Email, @Status, @PasswordHash, @CreatedAt, @DeletedAt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@Username", SqlDbType.NVarChar, 64) { Value = user.Username });
        command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = user.Email });
        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 32) { Value = user.State.ToString().ToUpperInvariant() });
        command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 128) { Value = user.PasswordHash });
        command.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = user.CreatedAt.UtcDateTime });
        command.Parameters.Add(new SqlParameter("@DeletedAt", SqlDbType.DateTime2) { Value = (object?)user.DeletedAt?.UtcDateTime ?? DBNull.Value });

        try
        {
            var scalarResult = await command.ExecuteScalarAsync(cancellationToken);
            if (scalarResult is int id)
            {
                user.SetId(id);
                return true;
            }

            return false;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            // Unique key violation (duplicate email)
            return false;
        }
    }

    public async Task<bool> TryReplacePasswordHashAsync(
        int userId,
        string expectedPasswordHash,
        string newPasswordHash,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE [User]
            SET [PasswordHash] = @NewPasswordHash
            WHERE [Id] = @UserId AND [PasswordHash] = @ExpectedPasswordHash;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@NewPasswordHash", SqlDbType.NVarChar, 128) { Value = newPasswordHash });
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
        command.Parameters.Add(new SqlParameter("@ExpectedPasswordHash", SqlDbType.NVarChar, 128) { Value = expectedPasswordHash });

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }
}
