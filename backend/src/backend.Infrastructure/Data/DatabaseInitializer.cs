using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Data;

public sealed class DatabaseInitializer(IOptions<DatabaseOptions> options, ILogger<DatabaseInitializer> logger)
{
    private readonly DatabaseOptions _options = options.Value;
    private readonly ILogger<DatabaseInitializer> _logger = logger;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _logger.LogWarning("Connection string is empty. Skipping database initialization.");
            return;
        }

        var builder = new SqlConnectionStringBuilder(_options.ConnectionString);
        var targetDatabase = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(targetDatabase))
        {
            targetDatabase = "RejkaDb";
            builder.InitialCatalog = targetDatabase;
        }

        var masterBuilder = new SqlConnectionStringBuilder(_options.ConnectionString)
        {
            InitialCatalog = "master"
        };

        const int maxRetries = 15;
        var retryDelay = TimeSpan.FromSeconds(2);
        var connected = false;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Connecting to SQL Server (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);

                await using var masterConnection = new SqlConnection(masterBuilder.ConnectionString);
                await masterConnection.OpenAsync(cancellationToken);

                var checkDbCmd = masterConnection.CreateCommand();
                checkDbCmd.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{targetDatabase}') CREATE DATABASE [{targetDatabase}];";
                await checkDbCmd.ExecuteNonQueryAsync(cancellationToken);

                _logger.LogInformation("Database '{DatabaseName}' verified/created.", targetDatabase);
                connected = true;
                break;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning("SQL Server not ready yet (attempt {Attempt}/{MaxRetries}): {Message}. Retrying...", attempt, maxRetries, ex.Message);
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        if (!connected)
        {
            _logger.LogError("Could not connect to SQL Server after {MaxRetries} attempts.", maxRetries);
            return;
        }

        // Now connect to target database and create tables
        await using var targetConnection = new SqlConnection(_options.ConnectionString);
        await targetConnection.OpenAsync(cancellationToken);

        const string schemaSql = """
            -- 1. User
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'User')
            BEGIN
                CREATE TABLE [User] (
                    [Id] INTEGER NOT NULL IDENTITY,
                    [Username] NVARCHAR(64) NOT NULL,
                    [Email] NVARCHAR(255) NOT NULL UNIQUE,
                    [Status] NVARCHAR(32) NOT NULL,
                    [PasswordHash] NVARCHAR(128) NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL,
                    [DeletedAt] DATETIME2,
                    PRIMARY KEY([Id])
                );
            END;

            -- 2. Geography
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Geography')
            BEGIN
                CREATE TABLE [Geography] (
                    [Id] INTEGER NOT NULL IDENTITY,
                    [latitude] DECIMAL(8,6) NOT NULL,
                    [longitude] DECIMAL(9,6) NOT NULL,
                    PRIMARY KEY([Id])
                );
            END;

            -- 3. Media
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Media')
            BEGIN
                CREATE TABLE [Media] (
                    [Id] INTEGER NOT NULL IDENTITY,
                    [UserId] INTEGER NOT NULL,
                    [LocationId] INTEGER,
                    [RecordedAt] DATETIME2,
                    [UploadedAt] DATETIME2 NOT NULL,
                    [Comment] NVARCHAR(MAX),
                    [Status] NVARCHAR(32) NOT NULL,
                    [MediaUrl] NVARCHAR(2048),
                    [FileName] NVARCHAR(255) NOT NULL,
                    PRIMARY KEY([Id]),
                    CONSTRAINT FK_Media_User FOREIGN KEY([UserId]) REFERENCES [User]([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION,
                    CONSTRAINT FK_Media_Geography FOREIGN KEY([LocationId]) REFERENCES [Geography]([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION
                );
            END;

            -- 4. Registration
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Registration')
            BEGIN
                CREATE TABLE [Registration] (
                    [Id] INTEGER NOT NULL IDENTITY,
                    [Number] NVARCHAR(16) NOT NULL UNIQUE,
                    PRIMARY KEY([Id])
                );
            END;

            -- 5. Rating
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Rating')
            BEGIN
                CREATE TABLE [Rating] (
                    [RegistrationId] INTEGER NOT NULL,
                    [MediaId] INTEGER NOT NULL,
                    [Rating] INTEGER,
                    PRIMARY KEY([RegistrationId], [MediaId]),
                    CONSTRAINT FK_Rating_Registration FOREIGN KEY([RegistrationId]) REFERENCES [Registration]([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION,
                    CONSTRAINT FK_Rating_Media FOREIGN KEY([MediaId]) REFERENCES [Media]([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION
                );
            END;

            -- 6. UserActivityRecords
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserActivityRecords')
            BEGIN
                CREATE TABLE [UserActivityRecords] (
                    [Id] INTEGER NOT NULL IDENTITY,
                    [UserId] INTEGER NOT NULL,
                    [Action] NVARCHAR(64) NOT NULL,
                    [OccurredAt] DATETIME2 NOT NULL,
                    PRIMARY KEY([Id]),
                    CONSTRAINT FK_UserActivity_User FOREIGN KEY([UserId]) REFERENCES [User]([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION
                );
            END;

            -- 7. MediaActivityRecords
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MediaActivityRecords')
            BEGIN
                CREATE TABLE [MediaActivityRecords] (
                    [Id] INTEGER NOT NULL IDENTITY,
                    [MediaId] INTEGER NOT NULL,
                    [Action] NVARCHAR(64) NOT NULL,
                    [OccurredAt] DATETIME2 NOT NULL,
                    PRIMARY KEY([Id]),
                    CONSTRAINT FK_MediaActivity_Media FOREIGN KEY([MediaId]) REFERENCES [Media]([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION
                );
            END;

            -- 8. UserRefreshSessions
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRefreshSessions')
            BEGIN
                CREATE TABLE [UserRefreshSessions] (
                    [Id] INTEGER NOT NULL IDENTITY,
                    [UserId] INTEGER NOT NULL,
                    [TokenHash] NVARCHAR(128) NOT NULL UNIQUE,
                    [CreatedAt] DATETIME2 NOT NULL,
                    [ExpiresAt] DATETIME2 NOT NULL,
                    [RevokedAt] DATETIME2,
                    [ReplacedByTokenHash] NVARCHAR(128),
                    PRIMARY KEY([Id]),
                    CONSTRAINT FK_RefreshSession_User FOREIGN KEY([UserId]) REFERENCES [User]([Id]) ON DELETE CASCADE
                );
            END;
            """;

        using var schemaCmd = targetConnection.CreateCommand();
        schemaCmd.CommandText = schemaSql;
        await schemaCmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Database tables verified/created successfully.");
    }
}
