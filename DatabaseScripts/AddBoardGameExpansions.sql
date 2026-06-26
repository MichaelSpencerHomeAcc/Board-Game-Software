SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('bgd.BoardGame', 'IsExpansion') IS NULL
    BEGIN
        ALTER TABLE [bgd].[BoardGame]
            ADD [IsExpansion] bit NOT NULL
                CONSTRAINT [DF_bgd_BoardGame_IsExpansion] DEFAULT (0);
    END;

    IF OBJECT_ID('bgd.BoardGameExpansion', 'U') IS NULL
    BEGIN
        CREATE TABLE [bgd].[BoardGameExpansion]
        (
            [ID] bigint IDENTITY(1,1) NOT NULL,
            [GID] uniqueidentifier NOT NULL
                CONSTRAINT [DF_bgd_BoardGameExpansion_GID] DEFAULT (NEWID()),
            [Inactive] bit NOT NULL
                CONSTRAINT [DF_bgd_BoardGameExpansion_Inactive] DEFAULT (0),
            [VersionStamp] rowversion NULL,
            [CreatedBy] nvarchar(128) NOT NULL,
            [TimeCreated] datetime NOT NULL,
            [ModifiedBy] nvarchar(128) NOT NULL,
            [TimeModified] datetime NOT NULL,
            [FK_bgd_BoardGame] bigint NOT NULL,
            [FK_bgd_ExpansionBoardGame] bigint NOT NULL,
            CONSTRAINT [PK_bgd_BoardGameExpansion]
                PRIMARY KEY CLUSTERED ([ID] ASC),
            CONSTRAINT [FK_bgd_BoardGameExpansion__bgd_BoardGame]
                FOREIGN KEY ([FK_bgd_BoardGame])
                REFERENCES [bgd].[BoardGame] ([ID]),
            CONSTRAINT [FK_bgd_BoardGameExpansion__bgd_ExpansionBoardGame]
                FOREIGN KEY ([FK_bgd_ExpansionBoardGame])
                REFERENCES [bgd].[BoardGame] ([ID]),
            CONSTRAINT [CK_bgd_BoardGameExpansion_NotSelf]
                CHECK ([FK_bgd_BoardGame] <> [FK_bgd_ExpansionBoardGame])
        );
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = 'AK_bgd_BoardGameExpansion_GID'
          AND [object_id] = OBJECT_ID('bgd.BoardGameExpansion')
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX [AK_bgd_BoardGameExpansion_GID]
            ON [bgd].[BoardGameExpansion] ([GID] ASC);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = 'IX_BoardGameExpansion_FK_bgd_ExpansionBoardGame'
          AND [object_id] = OBJECT_ID('bgd.BoardGameExpansion')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_BoardGameExpansion_FK_bgd_ExpansionBoardGame]
            ON [bgd].[BoardGameExpansion] ([FK_bgd_ExpansionBoardGame] ASC);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = 'UQ_bgd_BoardGameExpansion_FK_bgd_BoardGame_FK_bgd_ExpansionBoardGame'
          AND [object_id] = OBJECT_ID('bgd.BoardGameExpansion')
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX [UQ_bgd_BoardGameExpansion_FK_bgd_BoardGame_FK_bgd_ExpansionBoardGame]
            ON [bgd].[BoardGameExpansion] ([FK_bgd_BoardGame] ASC, [FK_bgd_ExpansionBoardGame] ASC);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

/*
Rollback, if needed:

BEGIN TRANSACTION;

IF OBJECT_ID('bgd.BoardGameExpansion', 'U') IS NOT NULL
    DROP TABLE [bgd].[BoardGameExpansion];

IF COL_LENGTH('bgd.BoardGame', 'IsExpansion') IS NOT NULL
    ALTER TABLE [bgd].[BoardGame] DROP COLUMN [IsExpansion];

COMMIT TRANSACTION;
*/
