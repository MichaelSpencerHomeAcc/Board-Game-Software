/*
    Move existing shelves under Mike's Clubhouse and repoint shelf game links
    from global template board games to Mike's club-owned board game copies.

    Run with @Commit = 0 first. If the printed row counts look right, set @Commit = 1.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ClubName nvarchar(120) = N'Mike''s Clubhouse';
DECLARE @Actor nvarchar(128) = N'shelf club migration';
DECLARE @Now datetime = GETUTCDATE();
DECLARE @Commit bit = 0;

DECLARE @ClubId bigint;

SELECT @ClubId = c.ID
FROM bgd.Club AS c
WHERE c.ClubName = @ClubName
  AND c.Inactive = 0;

IF @ClubId IS NULL
BEGIN
    THROW 51000, 'Could not find active club named Mike''s Clubhouse.', 1;
END;

BEGIN TRANSACTION;

IF COL_LENGTH('bgd.Shelf', 'FK_bgd_Club') IS NULL
BEGIN
    ALTER TABLE bgd.Shelf ADD FK_bgd_Club bigint NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'bgd.Shelf')
      AND name = N'IX_bgd_Shelf_FK_bgd_Club'
)
BEGIN
    CREATE INDEX IX_bgd_Shelf_FK_bgd_Club ON bgd.Shelf (FK_bgd_Club);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'bgd.Shelf')
      AND name = N'FK_bgd_Shelf__bgd_Club'
)
BEGIN
    ALTER TABLE bgd.Shelf WITH CHECK
    ADD CONSTRAINT FK_bgd_Shelf__bgd_Club
        FOREIGN KEY (FK_bgd_Club) REFERENCES bgd.Club (ID);
END;

UPDATE s
SET FK_bgd_Club = @ClubId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.Shelf AS s
WHERE s.FK_bgd_Club IS NULL;

PRINT CONCAT('Shelves assigned to Mike''s Clubhouse: ', @@ROWCOUNT);

DECLARE @GameMap TABLE (
    TemplateBoardGameId bigint NOT NULL PRIMARY KEY,
    ClubBoardGameId bigint NOT NULL
);

INSERT INTO @GameMap (TemplateBoardGameId, ClubBoardGameId)
SELECT template.ID,
       clubGame.ID
FROM bgd.BoardGame AS template
INNER JOIN bgd.BoardGame AS clubGame
    ON clubGame.FK_bgd_TemplateBoardGame = template.ID
   AND clubGame.FK_bgd_Club = @ClubId
WHERE template.FK_bgd_Club IS NULL
  AND template.Inactive = 0
  AND clubGame.Inactive = 0;

PRINT CONCAT('Template-to-club game mappings available: ', @@ROWCOUNT);

;WITH RepointCandidates AS (
    SELECT bgss.ID AS LinkId,
           map.ClubBoardGameId,
           bgss.FK_bgd_ShelfSection
    FROM bgd.BoardGameShelfSection AS bgss
    INNER JOIN bgd.ShelfSection AS ss
        ON ss.ID = bgss.FK_bgd_ShelfSection
    INNER JOIN bgd.Shelf AS shelf
        ON shelf.ID = ss.FK_bgd_Shelf
    INNER JOIN @GameMap AS map
        ON map.TemplateBoardGameId = bgss.FK_bgd_BoardGame
    WHERE shelf.FK_bgd_Club = @ClubId
      AND bgss.Inactive = 0
),
DuplicateLinks AS (
    SELECT candidate.LinkId
    FROM RepointCandidates AS candidate
    WHERE EXISTS (
        SELECT 1
        FROM bgd.BoardGameShelfSection AS existing
        WHERE existing.FK_bgd_BoardGame = candidate.ClubBoardGameId
          AND existing.FK_bgd_ShelfSection = candidate.FK_bgd_ShelfSection
          AND existing.ID <> candidate.LinkId
          AND existing.Inactive = 0
    )
)
DELETE bgss
FROM bgd.BoardGameShelfSection AS bgss
INNER JOIN DuplicateLinks AS duplicate
    ON duplicate.LinkId = bgss.ID;

PRINT CONCAT('Duplicate shelf links removed before repointing: ', @@ROWCOUNT);

UPDATE bgss
SET FK_bgd_BoardGame = map.ClubBoardGameId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.BoardGameShelfSection AS bgss
INNER JOIN bgd.ShelfSection AS ss
    ON ss.ID = bgss.FK_bgd_ShelfSection
INNER JOIN bgd.Shelf AS shelf
    ON shelf.ID = ss.FK_bgd_Shelf
INNER JOIN @GameMap AS map
    ON map.TemplateBoardGameId = bgss.FK_bgd_BoardGame
WHERE shelf.FK_bgd_Club = @ClubId
  AND bgss.Inactive = 0;

PRINT CONCAT('Shelf game links repointed to club copies: ', @@ROWCOUNT);

SELECT
    ShelfCount = COUNT(DISTINCT shelf.ID),
    ShelfGameLinks = COUNT(bgss.ID),
    LinksStillPointingAtTemplates = SUM(CASE WHEN bg.FK_bgd_Club IS NULL THEN 1 ELSE 0 END),
    LinksPointingAtClubCopies = SUM(CASE WHEN bg.FK_bgd_Club = @ClubId THEN 1 ELSE 0 END)
FROM bgd.Shelf AS shelf
LEFT JOIN bgd.ShelfSection AS ss
    ON ss.FK_bgd_Shelf = shelf.ID
LEFT JOIN bgd.BoardGameShelfSection AS bgss
    ON bgss.FK_bgd_ShelfSection = ss.ID
   AND bgss.Inactive = 0
LEFT JOIN bgd.BoardGame AS bg
    ON bg.ID = bgss.FK_bgd_BoardGame
WHERE shelf.FK_bgd_Club = @ClubId
  AND shelf.Inactive = 0;

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT 'Committed shelf club migration.';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'Rolled back. Set @Commit = 1 after reviewing the counts.';
END;
