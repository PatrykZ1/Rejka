-- Rejka Database Initialization Script
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RejkaDb')
BEGIN
    CREATE DATABASE [RejkaDb];
END
GO

USE [RejkaDb];
GO

-- 1. User table
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
END
GO

-- 2. Geography table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Geography')
BEGIN
    CREATE TABLE [Geography] (
        [Id] INTEGER NOT NULL IDENTITY,
        [latitude] DECIMAL(8,6) NOT NULL,
        [longitude] DECIMAL(9,6) NOT NULL,
        PRIMARY KEY([Id])
    );
END
GO

-- 3. Media table
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
END
GO

-- 4. Registration table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Registration')
BEGIN
    CREATE TABLE [Registration] (
        [Id] INTEGER NOT NULL IDENTITY,
        [Number] NVARCHAR(16) NOT NULL UNIQUE,
        PRIMARY KEY([Id])
    );
END
GO

-- 5. Rating table (junction table between Registration and Media)
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
END
GO

-- 6. UserActivityRecords table
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
END
GO

-- 7. MediaActivityRecords table
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
END
GO

-- 8. UserRefreshSessions table (Supports JWT token rotation & revocation)
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
END
GO
