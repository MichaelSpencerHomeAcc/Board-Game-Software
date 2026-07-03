/*
    Deduplicate board games inside one club.

    Default behavior:
    - Finds active duplicate board games with the same name and expansion flag in @ClubName.
    - Keeps the row with the most dependent data, then lowest ID as a tie-breaker.
    - Repoints matches, votes, ratings, shelves, markers, images, and config rows.
    - Deletes the duplicate bgd.BoardGame rows.

    Run once with @Commit = 0, review the printed row counts, then set @Commit = 1.
    Set @IncludePlatformTemplates = 1 only if you also want to clean duplicates in the
    global template catalogue where FK_bgd_Club is NULL.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ClubName nvarchar(120) = N'Mike''s Clubhouse';
DECLARE @IncludePlatformTemplates bit = 0;
DECLARE @Commit bit = 0;
DECLARE @Actor nvarchar(128) = N'board game dedupe';
DECLARE @Now datetime = GETUTCDATE();
DECLARE @ClubId bigint = NULL;

SELECT @ClubId = c.ID
FROM bgd.Club AS c
WHERE c.ClubName = @ClubName
  AND c.Inactive = 0;

IF @ClubId IS NULL
BEGIN
    THROW 51000, 'Could not find the requested active club.', 1;
END;

IF OBJECT_ID('tempdb..#GameMap') IS NOT NULL DROP TABLE #GameMap;

;WITH Candidates AS
(
    SELECT
        bg.ID,
        bg.BoardGameName,
        bg.IsExpansion,
        bg.FK_bgd_Club,
        DependentRows =
            (SELECT COUNT_BIG(*) FROM bgd.BoardGameMatch x WHERE x.FK_bgd_BoardGame = bg.ID) +
            (SELECT COUNT_BIG(*) FROM bgd.BoardGameVote x WHERE x.FK_bgd_BoardGame = bg.ID) +
            (SELECT COUNT_BIG(*) FROM bgd.PlayerBoardGame x WHERE x.FK_bgd_BoardGame = bg.ID) +
            (SELECT COUNT_BIG(*) FROM bgd.PlayerBoardGameRating x WHERE x.FK_bgd_BoardGame = bg.ID) +
            (SELECT COUNT_BIG(*) FROM bgd.PlayerBoardGameStarRating x WHERE x.FK_bgd_BoardGame = bg.ID) +
            (SELECT COUNT_BIG(*) FROM bgd.BoardGameShelfSection x WHERE x.FK_bgd_BoardGame = bg.ID) +
            (SELECT COUNT_BIG(*) FROM bgd.BoardGameMarker x WHERE x.FK_bgd_BoardGame = bg.ID) +
            (SELECT COUNT_BIG(*) FROM bgd.BoardGameResult x WHERE x.FK_bgd_BoardGame = bg.ID) +
            (SELECT COUNT_BIG(*) FROM bgd.BoardGameEloMethod x WHERE x.FK_bgd_BoardGame = bg.ID) +
            (SELECT COUNT_BIG(*) FROM bgd.BoardGameExpansion x WHERE x.FK_bgd_BoardGame = bg.ID OR x.FK_bgd_ExpansionBoardGame = bg.ID) +
            (SELECT COUNT_BIG(*) FROM bgd.StoredImage x WHERE x.OwnerType = N'GameCover' AND x.OwnerId = bg.ID)
    FROM bgd.BoardGame AS bg
    WHERE bg.Inactive = 0
      AND
      (
          bg.FK_bgd_Club = @ClubId
          OR (@IncludePlatformTemplates = 1 AND bg.FK_bgd_Club IS NULL)
      )
),
DuplicateGroups AS
(
    SELECT
        BoardGameName,
        IsExpansion,
        FK_bgd_Club
    FROM Candidates
    GROUP BY BoardGameName, IsExpansion, FK_bgd_Club
    HAVING COUNT(*) > 1
),
Ranked AS
(
    SELECT
        c.*,
        KeepId = FIRST_VALUE(c.ID) OVER
        (
            PARTITION BY c.BoardGameName, c.IsExpansion, c.FK_bgd_Club
            ORDER BY c.DependentRows DESC, c.ID ASC
        ),
        RowNumber = ROW_NUMBER() OVER
        (
            PARTITION BY c.BoardGameName, c.IsExpansion, c.FK_bgd_Club
            ORDER BY c.DependentRows DESC, c.ID ASC
        )
    FROM Candidates AS c
    INNER JOIN DuplicateGroups AS dg
        ON dg.BoardGameName = c.BoardGameName
       AND dg.IsExpansion = c.IsExpansion
       AND
       (
           dg.FK_bgd_Club = c.FK_bgd_Club
           OR (dg.FK_bgd_Club IS NULL AND c.FK_bgd_Club IS NULL)
       )
)
SELECT
    DuplicateId = ID,
    KeepId,
    BoardGameName,
    IsExpansion,
    FK_bgd_Club,
    DependentRows
INTO #GameMap
FROM Ranked
WHERE RowNumber > 1;

SELECT
    BoardGameName,
    IsExpansion,
    FK_bgd_Club,
    KeepId,
    DuplicateCount = COUNT(*)
FROM #GameMap
GROUP BY BoardGameName, IsExpansion, FK_bgd_Club, KeepId
ORDER BY BoardGameName, FK_bgd_Club;

IF NOT EXISTS (SELECT 1 FROM #GameMap)
BEGIN
    PRINT 'No duplicate board-game rows found for the requested scope.';
    RETURN;
END;

BEGIN TRANSACTION;

UPDATE image
SET OwnerId = map.KeepId
FROM bgd.StoredImage AS image
INNER JOIN #GameMap AS map
    ON image.OwnerType = N'GameCover'
   AND image.OwnerId = map.DuplicateId;

PRINT CONCAT('StoredImage rows repointed: ', @@ROWCOUNT);

UPDATE bg
SET FK_bgd_TemplateBoardGame = map.KeepId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.BoardGame AS bg
INNER JOIN #GameMap AS map
    ON bg.FK_bgd_TemplateBoardGame = map.DuplicateId;

PRINT CONCAT('Template references repointed: ', @@ROWCOUNT);

DELETE target
FROM bgd.BoardGameResult AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId
WHERE EXISTS
(
    SELECT 1
    FROM bgd.BoardGameResult AS existing
    WHERE existing.FK_bgd_BoardGame = map.KeepId
      AND existing.FK_bgd_ResultType = target.FK_bgd_ResultType
      AND existing.ID <> target.ID
);

PRINT CONCAT('Duplicate result config rows deleted: ', @@ROWCOUNT);

UPDATE target
SET FK_bgd_BoardGame = map.KeepId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.BoardGameResult AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId;

PRINT CONCAT('Result config rows repointed: ', @@ROWCOUNT);

DELETE target
FROM bgd.BoardGameEloMethod AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId
WHERE EXISTS
(
    SELECT 1
    FROM bgd.BoardGameEloMethod AS existing
    WHERE existing.FK_bgd_BoardGame = map.KeepId
      AND existing.FK_bgd_EloMethod = target.FK_bgd_EloMethod
      AND existing.ID <> target.ID
);

PRINT CONCAT('Duplicate ELO config rows deleted: ', @@ROWCOUNT);

UPDATE target
SET FK_bgd_BoardGame = map.KeepId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.BoardGameEloMethod AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId;

PRINT CONCAT('ELO config rows repointed: ', @@ROWCOUNT);

UPDATE mp
SET FK_bgd_BoardGameMarker = keeper.ID,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.BoardGameMatchPlayer AS mp
INNER JOIN bgd.BoardGameMarker AS duplicateMarker
    ON duplicateMarker.ID = mp.FK_bgd_BoardGameMarker
INNER JOIN #GameMap AS map
    ON map.DuplicateId = duplicateMarker.FK_bgd_BoardGame
INNER JOIN bgd.BoardGameMarker AS keeper
    ON keeper.FK_bgd_BoardGame = map.KeepId
   AND
   (
       keeper.FK_bgd_BoardGameMarkerType = duplicateMarker.FK_bgd_BoardGameMarkerType
       OR (keeper.FK_bgd_BoardGameMarkerType IS NULL AND duplicateMarker.FK_bgd_BoardGameMarkerType IS NULL)
   );

PRINT CONCAT('Match player marker references repointed: ', @@ROWCOUNT);

DELETE duplicateMarker
FROM bgd.BoardGameMarker AS duplicateMarker
INNER JOIN #GameMap AS map
    ON map.DuplicateId = duplicateMarker.FK_bgd_BoardGame
WHERE EXISTS
(
    SELECT 1
    FROM bgd.BoardGameMarker AS keeper
    WHERE keeper.FK_bgd_BoardGame = map.KeepId
      AND
      (
          keeper.FK_bgd_BoardGameMarkerType = duplicateMarker.FK_bgd_BoardGameMarkerType
          OR (keeper.FK_bgd_BoardGameMarkerType IS NULL AND duplicateMarker.FK_bgd_BoardGameMarkerType IS NULL)
      )
);

PRINT CONCAT('Duplicate marker config rows deleted: ', @@ROWCOUNT);

UPDATE target
SET FK_bgd_BoardGame = map.KeepId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.BoardGameMarker AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId;

PRINT CONCAT('Marker config rows repointed: ', @@ROWCOUNT);

IF OBJECT_ID('tempdb..#ExpansionTarget') IS NOT NULL DROP TABLE #ExpansionTarget;

SELECT
    expansion.ID,
    NewBaseGameId = COALESCE(baseMap.KeepId, expansion.FK_bgd_BoardGame),
    NewExpansionGameId = COALESCE(expansionMap.KeepId, expansion.FK_bgd_ExpansionBoardGame),
    IsAlreadyCorrect = CASE
        WHEN baseMap.KeepId IS NULL AND expansionMap.KeepId IS NULL THEN 1
        ELSE 0
    END
INTO #ExpansionTarget
FROM bgd.BoardGameExpansion AS expansion
LEFT JOIN #GameMap AS baseMap
    ON baseMap.DuplicateId = expansion.FK_bgd_BoardGame
LEFT JOIN #GameMap AS expansionMap
    ON expansionMap.DuplicateId = expansion.FK_bgd_ExpansionBoardGame
WHERE baseMap.DuplicateId IS NOT NULL
   OR expansionMap.DuplicateId IS NOT NULL;

DELETE expansion
FROM bgd.BoardGameExpansion AS expansion
INNER JOIN #ExpansionTarget AS target
    ON target.ID = expansion.ID
WHERE EXISTS
(
    SELECT 1
    FROM bgd.BoardGameExpansion AS existing
    WHERE existing.ID <> expansion.ID
      AND existing.FK_bgd_BoardGame = target.NewBaseGameId
      AND existing.FK_bgd_ExpansionBoardGame = target.NewExpansionGameId
);

PRINT CONCAT('Expansion links deleted because keeper links already existed: ', @@ROWCOUNT);

DELETE target
FROM #ExpansionTarget AS target
WHERE NOT EXISTS
(
    SELECT 1
    FROM bgd.BoardGameExpansion AS expansion
    WHERE expansion.ID = target.ID
);

;WITH RankedExpansion AS
(
    SELECT
        *,
        RowNumber = ROW_NUMBER() OVER
        (
            PARTITION BY NewBaseGameId, NewExpansionGameId
            ORDER BY IsAlreadyCorrect DESC, ID ASC
        )
    FROM #ExpansionTarget
)
DELETE expansion
FROM bgd.BoardGameExpansion AS expansion
INNER JOIN RankedExpansion AS ranked
    ON ranked.ID = expansion.ID
WHERE ranked.RowNumber > 1;

PRINT CONCAT('Duplicate expansion links deleted: ', @@ROWCOUNT);

UPDATE expansion
SET FK_bgd_BoardGame = target.NewBaseGameId,
    FK_bgd_ExpansionBoardGame = target.NewExpansionGameId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.BoardGameExpansion AS expansion
INNER JOIN #ExpansionTarget AS target
    ON target.ID = expansion.ID;

PRINT CONCAT('Expansion links repointed: ', @@ROWCOUNT);

DELETE target
FROM bgd.BoardGameShelfSection AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId
WHERE EXISTS
(
    SELECT 1
    FROM bgd.BoardGameShelfSection AS existing
    WHERE existing.FK_bgd_BoardGame = map.KeepId
      AND existing.FK_bgd_ShelfSection = target.FK_bgd_ShelfSection
      AND existing.ID <> target.ID
);

PRINT CONCAT('Duplicate shelf links deleted: ', @@ROWCOUNT);

UPDATE target
SET FK_bgd_BoardGame = map.KeepId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.BoardGameShelfSection AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId;

PRINT CONCAT('Shelf links repointed: ', @@ROWCOUNT);

DELETE target
FROM bgd.PlayerBoardGameRating AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId
WHERE EXISTS
(
    SELECT 1
    FROM bgd.PlayerBoardGameRating AS existing
    WHERE existing.FK_bgd_Player = target.FK_bgd_Player
      AND existing.FK_bgd_BoardGame = map.KeepId
      AND existing.ID <> target.ID
);

PRINT CONCAT('Duplicate player ratings deleted: ', @@ROWCOUNT);

UPDATE target
SET FK_bgd_BoardGame = map.KeepId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.PlayerBoardGameRating AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId;

PRINT CONCAT('Player ratings repointed: ', @@ROWCOUNT);

DELETE target
FROM bgd.PlayerBoardGameStarRating AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId
WHERE EXISTS
(
    SELECT 1
    FROM bgd.PlayerBoardGameStarRating AS existing
    WHERE existing.FK_bgd_Player = target.FK_bgd_Player
      AND existing.FK_bgd_BoardGame = map.KeepId
      AND existing.ID <> target.ID
);

PRINT CONCAT('Duplicate player star ratings deleted: ', @@ROWCOUNT);

UPDATE target
SET FK_bgd_BoardGame = map.KeepId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.PlayerBoardGameStarRating AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId;

PRINT CONCAT('Player star ratings repointed: ', @@ROWCOUNT);

UPDATE target
SET FK_bgd_BoardGame = map.KeepId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.PlayerBoardGame AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId;

PRINT CONCAT('Player top-ten/game rows repointed: ', @@ROWCOUNT);

DELETE target
FROM bgd.BoardGameVote AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId
WHERE EXISTS
(
    SELECT 1
    FROM bgd.BoardGameVote AS existing
    WHERE existing.FK_bgd_BoardGameNight = target.FK_bgd_BoardGameNight
      AND existing.FK_bgd_Player = target.FK_bgd_Player
      AND existing.FK_bgd_BoardGame = map.KeepId
      AND existing.ID <> target.ID
);

PRINT CONCAT('Duplicate votes deleted: ', @@ROWCOUNT);

UPDATE target
SET FK_bgd_BoardGame = map.KeepId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.BoardGameVote AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId;

PRINT CONCAT('Votes repointed: ', @@ROWCOUNT);

UPDATE target
SET FK_bgd_BoardGame = map.KeepId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.BoardGameMatch AS target
INNER JOIN #GameMap AS map
    ON target.FK_bgd_BoardGame = map.DuplicateId;

PRINT CONCAT('Matches repointed: ', @@ROWCOUNT);

DELETE bg
FROM bgd.BoardGame AS bg
INNER JOIN #GameMap AS map
    ON map.DuplicateId = bg.ID;

PRINT CONCAT('Duplicate board-game rows deleted: ', @@ROWCOUNT);

SELECT
    RemainingDuplicateGroups = COUNT_BIG(*)
FROM
(
    SELECT BoardGameName, IsExpansion, FK_bgd_Club
    FROM bgd.BoardGame
    WHERE Inactive = 0
      AND
      (
          FK_bgd_Club = @ClubId
          OR (@IncludePlatformTemplates = 1 AND FK_bgd_Club IS NULL)
      )
    GROUP BY BoardGameName, IsExpansion, FK_bgd_Club
    HAVING COUNT(*) > 1
) AS remaining;

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT 'Committed board-game dedupe.';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'Rolled back. Set @Commit = 1 after reviewing the counts.';
END;
