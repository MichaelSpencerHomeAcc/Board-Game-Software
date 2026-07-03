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

