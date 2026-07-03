IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260212182427_SyncPlayerBoardGame'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260212182427_SyncPlayerBoardGame', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625161651_AddBoardGameExpansions'
)
BEGIN
    IF COL_LENGTH(N'bgd.BoardGame', N'IsExpansion') IS NULL
    BEGIN
        ALTER TABLE [bgd].[BoardGame] ADD [IsExpansion] bit NOT NULL CONSTRAINT [DF_bgd_BoardGame_IsExpansion] DEFAULT CAST(0 AS bit);
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625161651_AddBoardGameExpansions'
)
BEGIN
    IF OBJECT_ID(N'bgd.BoardGameExpansion', N'U') IS NULL
    BEGIN
        CREATE TABLE [bgd].[BoardGameExpansion] (
            [ID] bigint NOT NULL IDENTITY,
            [GID] uniqueidentifier NOT NULL CONSTRAINT [DF_bgd_BoardGameExpansion_GID] DEFAULT (newid()),
            [Inactive] bit NOT NULL,
            [VersionStamp] rowversion NULL,
            [CreatedBy] nvarchar(128) NOT NULL,
            [TimeCreated] datetime NOT NULL,
            [ModifiedBy] nvarchar(128) NOT NULL,
            [TimeModified] datetime NOT NULL,
            [FK_bgd_BoardGame] bigint NOT NULL,
            [FK_bgd_ExpansionBoardGame] bigint NOT NULL,
            CONSTRAINT [PK_bgd_BoardGameExpansion] PRIMARY KEY ([ID]),
            CONSTRAINT [FK_bgd_BoardGameExpansion__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
            CONSTRAINT [FK_bgd_BoardGameExpansion__bgd_ExpansionBoardGame] FOREIGN KEY ([FK_bgd_ExpansionBoardGame]) REFERENCES [bgd].[BoardGame] ([ID])
        );
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'AK_bgd_BoardGameExpansion_GID' AND [object_id] = OBJECT_ID(N'bgd.BoardGameExpansion'))
        CREATE UNIQUE INDEX [AK_bgd_BoardGameExpansion_GID] ON [bgd].[BoardGameExpansion] ([GID]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_BoardGameExpansion_FK_bgd_ExpansionBoardGame' AND [object_id] = OBJECT_ID(N'bgd.BoardGameExpansion'))
        CREATE INDEX [IX_BoardGameExpansion_FK_bgd_ExpansionBoardGame] ON [bgd].[BoardGameExpansion] ([FK_bgd_ExpansionBoardGame]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UQ_bgd_BoardGameExpansion_FK_bgd_BoardGame_FK_bgd_ExpansionBoardGame' AND [object_id] = OBJECT_ID(N'bgd.BoardGameExpansion'))
        CREATE UNIQUE INDEX [UQ_bgd_BoardGameExpansion_FK_bgd_BoardGame_FK_bgd_ExpansionBoardGame] ON [bgd].[BoardGameExpansion] ([FK_bgd_BoardGame], [FK_bgd_ExpansionBoardGame]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625161651_AddBoardGameExpansions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625161651_AddBoardGameExpansions', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626235134_AddBoardGameVotes2'
)
BEGIN
    CREATE TABLE [bgd].[BoardGameVote] (
        [ID] bigint NOT NULL IDENTITY,
        [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [Inactive] bit NOT NULL,
        [VersionStamp] rowversion NULL,
        [CreatedBy] nvarchar(128) NOT NULL,
        [TimeCreated] datetime NOT NULL,
        [ModifiedBy] nvarchar(128) NOT NULL,
        [TimeModified] datetime NOT NULL,
        [FK_bgd_BoardGameNight] bigint NOT NULL,
        [FK_bgd_BoardGame] bigint NOT NULL,
        [FK_bgd_Player] bigint NOT NULL,
        CONSTRAINT [PK_bgd_BoardGameVote] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_bgd_BoardGameVote__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
        CONSTRAINT [FK_bgd_BoardGameVote__bgd_BoardGameNight] FOREIGN KEY ([FK_bgd_BoardGameNight]) REFERENCES [bgd].[BoardGameNight] ([ID]),
        CONSTRAINT [FK_bgd_BoardGameVote__bgd_Player] FOREIGN KEY ([FK_bgd_Player]) REFERENCES [bgd].[Player] ([ID])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626235134_AddBoardGameVotes2'
)
BEGIN
    CREATE UNIQUE INDEX [AK_bgd_BoardGameVote_GID] ON [bgd].[BoardGameVote] ([GID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626235134_AddBoardGameVotes2'
)
BEGIN
    CREATE INDEX [IX_BoardGameVote_FK_bgd_BoardGame] ON [bgd].[BoardGameVote] ([FK_bgd_BoardGame]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626235134_AddBoardGameVotes2'
)
BEGIN
    CREATE INDEX [IX_BoardGameVote_FK_bgd_Player] ON [bgd].[BoardGameVote] ([FK_bgd_Player]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626235134_AddBoardGameVotes2'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_bgd_BoardGameVote_Night_Game_Player] ON [bgd].[BoardGameVote] ([FK_bgd_BoardGameNight], [FK_bgd_BoardGame], [FK_bgd_Player]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260626235134_AddBoardGameVotes2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260626235134_AddBoardGameVotes2', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    ALTER TABLE [bgd].[BoardGameVote] DROP CONSTRAINT [FK_bgd_BoardGameVote__bgd_BoardGame];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    ALTER TABLE [bgd].[BoardGameVote] DROP CONSTRAINT [FK_bgd_BoardGameVote__bgd_BoardGameNight];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    ALTER TABLE [bgd].[BoardGameVote] DROP CONSTRAINT [FK_bgd_BoardGameVote__bgd_Player];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    ALTER TABLE [bgd].[BoardGameVote] ADD CONSTRAINT [FK_bgd_BoardGameVote__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    ALTER TABLE [bgd].[BoardGameVote] ADD CONSTRAINT [FK_bgd_BoardGameVote__bgd_BoardGameNight] FOREIGN KEY ([FK_bgd_BoardGameNight]) REFERENCES [bgd].[BoardGameNight] ([ID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    ALTER TABLE [bgd].[BoardGameVote] ADD CONSTRAINT [FK_bgd_BoardGameVote__bgd_Player] FOREIGN KEY ([FK_bgd_Player]) REFERENCES [bgd].[Player] ([ID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    CREATE TABLE [bgd].[PlayerAchievement] (
        [ID] bigint NOT NULL IDENTITY,
        [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [Inactive] bit NOT NULL,
        [VersionStamp] rowversion NULL,
        [CreatedBy] nvarchar(128) NOT NULL,
        [TimeCreated] datetime NOT NULL,
        [ModifiedBy] nvarchar(128) NOT NULL,
        [TimeModified] datetime NOT NULL,
        [FK_bgd_Player] bigint NOT NULL,
        [BadgeCode] varchar(60) NOT NULL,
        [BadgeTitle] nvarchar(80) NOT NULL,
        [BadgeDetail] nvarchar(240) NOT NULL,
        [UnlockedAt] datetime NOT NULL,
        [FK_bgd_BoardGame] bigint NULL,
        [FK_bgd_BoardGameMatch] bigint NULL,
        [FK_bgd_BoardGameNight] bigint NULL,
        CONSTRAINT [PK_bgd_PlayerAchievement] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_bgd_PlayerAchievement__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
        CONSTRAINT [FK_bgd_PlayerAchievement__bgd_BoardGameMatch] FOREIGN KEY ([FK_bgd_BoardGameMatch]) REFERENCES [bgd].[BoardGameMatch] ([ID]),
        CONSTRAINT [FK_bgd_PlayerAchievement__bgd_BoardGameNight] FOREIGN KEY ([FK_bgd_BoardGameNight]) REFERENCES [bgd].[BoardGameNight] ([ID]),
        CONSTRAINT [FK_bgd_PlayerAchievement__bgd_Player] FOREIGN KEY ([FK_bgd_Player]) REFERENCES [bgd].[Player] ([ID])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    CREATE UNIQUE INDEX [AK_bgd_PlayerAchievement_GID] ON [bgd].[PlayerAchievement] ([GID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    CREATE INDEX [IX_PlayerAchievement_FK_bgd_BoardGame] ON [bgd].[PlayerAchievement] ([FK_bgd_BoardGame]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    CREATE INDEX [IX_PlayerAchievement_FK_bgd_BoardGameMatch] ON [bgd].[PlayerAchievement] ([FK_bgd_BoardGameMatch]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    CREATE INDEX [IX_PlayerAchievement_FK_bgd_BoardGameNight] ON [bgd].[PlayerAchievement] ([FK_bgd_BoardGameNight]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_bgd_PlayerAchievement_Scope] ON [bgd].[PlayerAchievement] ([FK_bgd_Player], [BadgeCode], [FK_bgd_BoardGame], [FK_bgd_BoardGameMatch], [FK_bgd_BoardGameNight]) WHERE [FK_bgd_BoardGame] IS NOT NULL AND [FK_bgd_BoardGameMatch] IS NOT NULL AND [FK_bgd_BoardGameNight] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627003059_AddPlayerAchievements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260627003059_AddPlayerAchievements', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    DROP INDEX [IX_PlayerBoardGame_FK_bgd_Player_Rank] ON [bgd].[PlayerBoardGame];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    ALTER TABLE [bgd].[Player] ADD [FK_bgd_Club] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    ALTER TABLE [bgd].[BoardGameNight] ADD [FK_bgd_Club] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    CREATE TABLE [bgd].[Club] (
        [ID] bigint NOT NULL IDENTITY,
        [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [Inactive] bit NOT NULL,
        [VersionStamp] rowversion NULL,
        [CreatedBy] nvarchar(128) NOT NULL,
        [TimeCreated] datetime NOT NULL,
        [ModifiedBy] nvarchar(128) NOT NULL,
        [TimeModified] datetime NOT NULL,
        [ClubName] varchar(120) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Slug] varchar(80) NULL,
        [ContactEmail] nvarchar(256) NULL,
        [VenueName] nvarchar(160) NULL,
        [VenueAddress] nvarchar(300) NULL,
        [Latitude] decimal(9,6) NULL,
        [Longitude] decimal(9,6) NULL,
        CONSTRAINT [PK_bgd_Club] PRIMARY KEY ([ID])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    CREATE TABLE [bgd].[ClubMembership] (
        [ID] bigint NOT NULL IDENTITY,
        [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [Inactive] bit NOT NULL,
        [VersionStamp] rowversion NULL,
        [CreatedBy] nvarchar(128) NOT NULL,
        [TimeCreated] datetime NOT NULL,
        [ModifiedBy] nvarchar(128) NOT NULL,
        [TimeModified] datetime NOT NULL,
        [FK_bgd_Club] bigint NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Role] varchar(30) NOT NULL,
        [JoinedAt] datetime NOT NULL,
        CONSTRAINT [PK_bgd_ClubMembership] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_bgd_ClubMembership__bgd_Club] FOREIGN KEY ([FK_bgd_Club]) REFERENCES [bgd].[Club] ([ID])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PlayerBoardGame_FK_bgd_Player_Rank] ON [bgd].[PlayerBoardGame] ([FK_bgd_Player], [Rank]) WHERE [FK_bgd_Player] IS NOT NULL AND [Inactive] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    CREATE INDEX [IX_Player_FK_bgd_Club] ON [bgd].[Player] ([FK_bgd_Club]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    CREATE INDEX [IX_BoardGameNight_FK_bgd_Club] ON [bgd].[BoardGameNight] ([FK_bgd_Club]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [AK_bgd_Club_GID] ON [bgd].[Club] ([GID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_bgd_Club_Slug] ON [bgd].[Club] ([Slug]) WHERE [Slug] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [AK_bgd_ClubMembership_GID] ON [bgd].[ClubMembership] ([GID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_bgd_ClubMembership_Club_User] ON [bgd].[ClubMembership] ([FK_bgd_Club], [UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    ALTER TABLE [bgd].[BoardGameNight] ADD CONSTRAINT [FK_bgd_BoardGameNight__bgd_Club] FOREIGN KEY ([FK_bgd_Club]) REFERENCES [bgd].[Club] ([ID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    ALTER TABLE [bgd].[Player] ADD CONSTRAINT [FK_bgd_Player__bgd_Club] FOREIGN KEY ([FK_bgd_Club]) REFERENCES [bgd].[Club] ([ID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702121151_AddClubTenancyFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702121151_AddClubTenancyFoundation', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702125931_AddPlayerClubMemberships'
)
BEGIN
    CREATE TABLE [bgd].[PlayerClub] (
        [ID] bigint NOT NULL IDENTITY,
        [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [Inactive] bit NOT NULL,
        [VersionStamp] rowversion NULL,
        [CreatedBy] nvarchar(128) NOT NULL,
        [TimeCreated] datetime NOT NULL,
        [ModifiedBy] nvarchar(128) NOT NULL,
        [TimeModified] datetime NOT NULL,
        [FK_bgd_Player] bigint NOT NULL,
        [FK_bgd_Club] bigint NOT NULL,
        [JoinedAt] datetime NOT NULL,
        CONSTRAINT [PK_bgd_PlayerClub] PRIMARY KEY ([ID]),
        CONSTRAINT [FK_bgd_PlayerClub__bgd_Club] FOREIGN KEY ([FK_bgd_Club]) REFERENCES [bgd].[Club] ([ID]),
        CONSTRAINT [FK_bgd_PlayerClub__bgd_Player] FOREIGN KEY ([FK_bgd_Player]) REFERENCES [bgd].[Player] ([ID])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702125931_AddPlayerClubMemberships'
)
BEGIN
    INSERT INTO [bgd].[PlayerClub]
        ([GID], [Inactive], [CreatedBy], [TimeCreated], [ModifiedBy], [TimeModified], [FK_bgd_Player], [FK_bgd_Club], [JoinedAt])
    SELECT
        NEWID(),
        0,
        COALESCE(NULLIF([CreatedBy], N''), N'EF migration'),
        COALESCE([TimeCreated], GETUTCDATE()),
        COALESCE(NULLIF([ModifiedBy], N''), N'EF migration'),
        GETUTCDATE(),
        [ID],
        [FK_bgd_Club],
        GETUTCDATE()
    FROM [bgd].[Player]
    WHERE [FK_bgd_Club] IS NOT NULL
        AND NOT EXISTS (
            SELECT 1
            FROM [bgd].[PlayerClub] pc
            WHERE pc.[FK_bgd_Player] = [bgd].[Player].[ID]
                AND pc.[FK_bgd_Club] = [bgd].[Player].[FK_bgd_Club]
        );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702125931_AddPlayerClubMemberships'
)
BEGIN
    CREATE UNIQUE INDEX [AK_bgd_PlayerClub_GID] ON [bgd].[PlayerClub] ([GID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702125931_AddPlayerClubMemberships'
)
BEGIN
    CREATE INDEX [IX_PlayerClub_FK_bgd_Club] ON [bgd].[PlayerClub] ([FK_bgd_Club]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702125931_AddPlayerClubMemberships'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_bgd_PlayerClub_Player_Club] ON [bgd].[PlayerClub] ([FK_bgd_Player], [FK_bgd_Club]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702125931_AddPlayerClubMemberships'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702125931_AddPlayerClubMemberships', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702201630_AddBoardGameClubTemplates'
)
BEGIN
    ALTER TABLE [bgd].[BoardGame] ADD [FK_bgd_Club] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702201630_AddBoardGameClubTemplates'
)
BEGIN
    ALTER TABLE [bgd].[BoardGame] ADD [FK_bgd_TemplateBoardGame] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702201630_AddBoardGameClubTemplates'
)
BEGIN
    CREATE INDEX [IX_bgd_BoardGame_FK_bgd_Club] ON [bgd].[BoardGame] ([FK_bgd_Club]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702201630_AddBoardGameClubTemplates'
)
BEGIN
    CREATE INDEX [IX_bgd_BoardGame_FK_bgd_TemplateBoardGame] ON [bgd].[BoardGame] ([FK_bgd_TemplateBoardGame]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702201630_AddBoardGameClubTemplates'
)
BEGIN
    ALTER TABLE [bgd].[BoardGame] ADD CONSTRAINT [FK_bgd_BoardGame__bgd_Club] FOREIGN KEY ([FK_bgd_Club]) REFERENCES [bgd].[Club] ([ID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702201630_AddBoardGameClubTemplates'
)
BEGIN
    ALTER TABLE [bgd].[BoardGame] ADD CONSTRAINT [FK_bgd_BoardGame__bgd_TemplateBoardGame] FOREIGN KEY ([FK_bgd_TemplateBoardGame]) REFERENCES [bgd].[BoardGame] ([ID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702201630_AddBoardGameClubTemplates'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702201630_AddBoardGameClubTemplates', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702211548_AddShelfClubOwnership'
)
BEGIN
    ALTER TABLE [bgd].[Shelf] ADD [FK_bgd_Club] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702211548_AddShelfClubOwnership'
)
BEGIN
    CREATE INDEX [IX_bgd_Shelf_FK_bgd_Club] ON [bgd].[Shelf] ([FK_bgd_Club]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702211548_AddShelfClubOwnership'
)
BEGIN
    ALTER TABLE [bgd].[Shelf] ADD CONSTRAINT [FK_bgd_Shelf__bgd_Club] FOREIGN KEY ([FK_bgd_Club]) REFERENCES [bgd].[Club] ([ID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702211548_AddShelfClubOwnership'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702211548_AddShelfClubOwnership', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702224500_AddClubScopedReferenceData'
)
BEGIN
    DROP INDEX [UQ_bgd_BoardGameMarkerType_TypeDesc] ON [bgd].[BoardGameMarkerType];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702224500_AddClubScopedReferenceData'
)
BEGIN
    ALTER TABLE [bgd].[Publisher] ADD [FK_bgd_Club] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702224500_AddClubScopedReferenceData'
)
BEGIN
    ALTER TABLE [bgd].[BoardGameMarkerType] ADD [FK_bgd_Club] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702224500_AddClubScopedReferenceData'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_bgd_Publisher_Club_PublisherName] ON [bgd].[Publisher] ([FK_bgd_Club], [PublisherName]) WHERE [FK_bgd_Club] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702224500_AddClubScopedReferenceData'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_bgd_Publisher_Global_PublisherName] ON [bgd].[Publisher] ([PublisherName]) WHERE [FK_bgd_Club] IS NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702224500_AddClubScopedReferenceData'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_bgd_BoardGameMarkerType_Club_TypeDesc] ON [bgd].[BoardGameMarkerType] ([FK_bgd_Club], [TypeDesc]) WHERE [FK_bgd_Club] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702224500_AddClubScopedReferenceData'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_bgd_BoardGameMarkerType_Global_TypeDesc] ON [bgd].[BoardGameMarkerType] ([TypeDesc]) WHERE [FK_bgd_Club] IS NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702224500_AddClubScopedReferenceData'
)
BEGIN
    ALTER TABLE [bgd].[BoardGameMarkerType] ADD CONSTRAINT [FK_bgd_BoardGameMarkerType__bgd_Club] FOREIGN KEY ([FK_bgd_Club]) REFERENCES [bgd].[Club] ([ID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702224500_AddClubScopedReferenceData'
)
BEGIN
    ALTER TABLE [bgd].[Publisher] ADD CONSTRAINT [FK_bgd_Publisher__bgd_Club] FOREIGN KEY ([FK_bgd_Club]) REFERENCES [bgd].[Club] ([ID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702224500_AddClubScopedReferenceData'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702224500_AddClubScopedReferenceData', N'8.0.18');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703020000_AddStoredImages'
)
BEGIN
    CREATE TABLE [bgd].[StoredImage] (
        [ID] int NOT NULL IDENTITY,
        [OwnerType] nvarchar(80) NOT NULL,
        [OwnerId] int NOT NULL,
        [BlobProvider] nvarchar(40) NOT NULL,
        [BlobKey] nvarchar(512) NOT NULL,
        [PublicUrl] nvarchar(1024) NOT NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(127) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [AltText] nvarchar(255) NULL,
        [Caption] nvarchar(500) NULL,
        [UploadedByUserId] nvarchar(450) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_bgd_StoredImage] PRIMARY KEY ([ID])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703020000_AddStoredImages'
)
BEGIN
    CREATE INDEX [IX_bgd_StoredImage_BlobKey] ON [bgd].[StoredImage] ([BlobKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703020000_AddStoredImages'
)
BEGIN
    CREATE INDEX [IX_bgd_StoredImage_CreatedAtUtc] ON [bgd].[StoredImage] ([CreatedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703020000_AddStoredImages'
)
BEGIN
    CREATE INDEX [IX_bgd_StoredImage_OwnerType_OwnerId] ON [bgd].[StoredImage] ([OwnerType], [OwnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703020000_AddStoredImages'
)
BEGIN
    CREATE INDEX [IX_bgd_StoredImage_UploadedByUserId] ON [bgd].[StoredImage] ([UploadedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703020000_AddStoredImages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260703020000_AddStoredImages', N'8.0.18');
END;
GO

COMMIT;
GO

