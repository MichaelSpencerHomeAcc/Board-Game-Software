/*
    Copy functional data from the old database into tironicus_BoardGames_EF_Rebuild.

    This deliberately does not preserve the old audit trail:
    - VersionStamp / rowversion columns are skipped.
    - CreatedBy / ModifiedBy are set to @MigrationActor.
    - TimeCreated / TimeModified are set to @MigratedAt.

    Run in SQLCMD mode so the SourceDb and TargetDb variables are expanded.
    Keep @Commit = 0 for the first run so the script validates and rolls back.
*/

:setvar SourceDb "tironicus_BoardGames"
:setvar TargetDb "tironicus_BoardGames_EF_Rebuild"

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @SourceDb sysname = N'$(SourceDb)';
DECLARE @TargetDb sysname = N'$(TargetDb)';
DECLARE @MigrationActor nvarchar(128) = N'EF rebuild migration';
DECLARE @MigratedAt datetime = GETUTCDATE();
DECLARE @Commit bit = 0;

IF DB_ID(@SourceDb) IS NULL
    THROW 51000, 'Source database was not found.', 1;

IF DB_ID(@TargetDb) IS NULL
    THROW 51001, 'Target database was not found.', 1;

CREATE TABLE #CopyTables
(
    SortOrder int NOT NULL PRIMARY KEY,
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL
);

INSERT INTO #CopyTables (SortOrder, SchemaName, TableName)
VALUES
    (10,  N'dbo', N'AspNetRoles'),
    (20,  N'dbo', N'AspNetUsers'),
    (30,  N'dbo', N'AspNetRoleClaims'),
    (40,  N'dbo', N'AspNetUserClaims'),
    (50,  N'dbo', N'AspNetUserLogins'),
    (60,  N'dbo', N'AspNetUserRoles'),
    (70,  N'dbo', N'AspNetUserTokens'),

    (100, N'bgd', N'BoardGameImageType'),
    (110, N'bgd', N'BoardGameNight'),
    (120, N'bgd', N'BoardGameType'),
    (130, N'bgd', N'BoardGameVictoryConditionType'),
    (140, N'bgd', N'EloMethod'),
    (150, N'bgd', N'MarkerAdditionalType'),
    (160, N'bgd', N'MarkerAlignmentType'),
    (170, N'bgd', N'Player'),
    (180, N'bgd', N'Publisher'),
    (190, N'bgd', N'RankingQueryStore'),
    (200, N'bgd', N'ReleaseVersion'),
    (210, N'bgd', N'ResultType'),
    (220, N'bgd', N'Shelf'),
    (230, N'bgd', N'BoardGameMarkerType'),
    (240, N'bgd', N'BoardGame'),
    (250, N'bgd', N'ShelfSection'),
    (260, N'bgd', N'BoardGameNightPlayer'),
    (270, N'bgd', N'BoardGameEloMethod'),
    (280, N'bgd', N'BoardGameExpansion'),
    (290, N'bgd', N'BoardGameMarker'),
    (300, N'bgd', N'BoardGameMatch'),
    (310, N'bgd', N'BoardGameResult'),
    (320, N'bgd', N'BoardGameVote'),
    (330, N'bgd', N'PlayerBoardGame'),
    (340, N'bgd', N'PlayerBoardGameRating'),
    (350, N'bgd', N'PlayerBoardGameStarRating'),
    (360, N'bgd', N'BoardGameShelfSection'),
    (370, N'bgd', N'BoardGameMatchPlayer'),
    (380, N'bgd', N'BoardGameNightBoardGameMatch'),
    (390, N'bgd', N'PlayerAchievement'),
    (400, N'bgd', N'BoardGameMatchPlayerResult');

BEGIN TRANSACTION;

DECLARE @SchemaName sysname;
DECLARE @TableName sysname;
DECLARE @Columns nvarchar(max);
DECLARE @SelectList nvarchar(max);
DECLARE @HasIdentity bit;
DECLARE @MetadataSql nvarchar(max);
DECLARE @CopySql nvarchar(max);
DECLARE @SourceObject nvarchar(776);
DECLARE @TargetObject nvarchar(776);

DECLARE copy_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT SchemaName, TableName
    FROM #CopyTables
    ORDER BY SortOrder;

OPEN copy_cursor;
FETCH NEXT FROM copy_cursor INTO @SchemaName, @TableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SourceObject = QUOTENAME(@SourceDb) + N'.' + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);
    SET @TargetObject = QUOTENAME(@TargetDb) + N'.' + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);

    SET @MetadataSql = N'
;WITH TargetColumns AS
(
    SELECT c.name, c.column_id, c.is_identity
    FROM ' + QUOTENAME(@TargetDb) + N'.sys.columns c
    INNER JOIN ' + QUOTENAME(@TargetDb) + N'.sys.objects o ON o.object_id = c.object_id
    INNER JOIN ' + QUOTENAME(@TargetDb) + N'.sys.schemas s ON s.schema_id = o.schema_id
    WHERE s.name = @SchemaName
      AND o.name = @TableName
      AND o.type = N''U''
      AND c.is_computed = 0
      AND c.name <> N''VersionStamp''
      AND EXISTS
      (
          SELECT 1
          FROM ' + QUOTENAME(@SourceDb) + N'.sys.columns sc
          INNER JOIN ' + QUOTENAME(@SourceDb) + N'.sys.objects so ON so.object_id = sc.object_id
          INNER JOIN ' + QUOTENAME(@SourceDb) + N'.sys.schemas ss ON ss.schema_id = so.schema_id
          WHERE ss.name = @SchemaName
            AND so.name = @TableName
            AND so.type = N''U''
            AND sc.name = c.name
      )
)
SELECT
    @ColumnsOut = STUFF((
        SELECT N'', '' + QUOTENAME(name)
        FROM TargetColumns
        ORDER BY column_id
        FOR XML PATH(N''''), TYPE
    ).value(N''.'', N''nvarchar(max)''), 1, 2, N''''),
    @SelectListOut = STUFF((
        SELECT N'', '' +
            CASE
                WHEN name IN (N''CreatedBy'', N''ModifiedBy'') THEN N''@MigrationActor''
                WHEN name IN (N''TimeCreated'', N''TimeModified'') THEN N''@MigratedAt''
                ELSE N''s.'' + QUOTENAME(name)
            END
        FROM TargetColumns
        ORDER BY column_id
        FOR XML PATH(N''''), TYPE
    ).value(N''.'', N''nvarchar(max)''), 1, 2, N''''),
    @HasIdentityOut = CASE WHEN EXISTS (SELECT 1 FROM TargetColumns WHERE is_identity = 1) THEN 1 ELSE 0 END;';

    EXEC sp_executesql
        @MetadataSql,
        N'@SchemaName sysname, @TableName sysname, @ColumnsOut nvarchar(max) OUTPUT, @SelectListOut nvarchar(max) OUTPUT, @HasIdentityOut bit OUTPUT',
        @SchemaName = @SchemaName,
        @TableName = @TableName,
        @ColumnsOut = @Columns OUTPUT,
        @SelectListOut = @SelectList OUTPUT,
        @HasIdentityOut = @HasIdentity OUTPUT;

    IF @Columns IS NULL OR LEN(@Columns) = 0
    BEGIN
        THROW 51002, 'No matching columns were found for a table in the copy list.', 1;
    END;

    SET @CopySql = N'
IF EXISTS (SELECT 1 FROM ' + @TargetObject + N')
    THROW 51003, ''Target table is not empty. Stop before copying duplicate data.'', 1;

PRINT ''Copying ' + @SchemaName + N'.' + @TableName + N''';
';

    IF @HasIdentity = 1
        SET @CopySql += N'SET IDENTITY_INSERT ' + @TargetObject + N' ON;
';

    SET @CopySql += N'INSERT INTO ' + @TargetObject + N' (' + @Columns + N')
SELECT ' + @SelectList + N'
FROM ' + @SourceObject + N' s;
';

    IF @HasIdentity = 1
        SET @CopySql += N'SET IDENTITY_INSERT ' + @TargetObject + N' OFF;
';

    EXEC sp_executesql
        @CopySql,
        N'@MigrationActor nvarchar(128), @MigratedAt datetime',
        @MigrationActor = @MigrationActor,
        @MigratedAt = @MigratedAt;

    FETCH NEXT FROM copy_cursor INTO @SchemaName, @TableName;
END;

CLOSE copy_cursor;
DEALLOCATE copy_cursor;

DECLARE @CountSql nvarchar(max) = N'';

SELECT @CountSql += N'
SELECT N''' + SchemaName + N'.' + TableName + N''' AS TableName,
       (SELECT COUNT_BIG(*) FROM ' + QUOTENAME(@SourceDb) + N'.' + QUOTENAME(SchemaName) + N'.' + QUOTENAME(TableName) + N') AS SourceRows,
       (SELECT COUNT_BIG(*) FROM ' + QUOTENAME(@TargetDb) + N'.' + QUOTENAME(SchemaName) + N'.' + QUOTENAME(TableName) + N') AS TargetRows;'
FROM #CopyTables
ORDER BY SortOrder;

EXEC sp_executesql @CountSql;

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT 'Copy committed.';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'Copy rolled back. Set @Commit = 1 after reviewing output.';
END;
