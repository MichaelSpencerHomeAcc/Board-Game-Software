/*
    Fix PlayerBoardGame top-10 uniqueness for migrated historical data.

    The old database contains duplicate player/rank rows where older rows are inactive.
    The rebuilt schema should enforce unique top-10 ranks only for active rows.
*/

IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.objects o ON o.object_id = i.object_id
    INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
    WHERE s.name = N'bgd'
      AND o.name = N'PlayerBoardGame'
      AND i.name = N'IX_PlayerBoardGame_FK_bgd_Player_Rank'
)
BEGIN
    DROP INDEX [IX_PlayerBoardGame_FK_bgd_Player_Rank] ON [bgd].[PlayerBoardGame];
END;
GO

CREATE UNIQUE INDEX [IX_PlayerBoardGame_FK_bgd_Player_Rank]
ON [bgd].[PlayerBoardGame] ([FK_bgd_Player], [Rank])
WHERE [FK_bgd_Player] IS NOT NULL AND [Inactive] = 0;
GO
