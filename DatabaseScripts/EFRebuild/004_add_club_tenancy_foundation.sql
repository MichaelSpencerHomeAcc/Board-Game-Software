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

