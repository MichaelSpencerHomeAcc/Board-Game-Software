/*
    Diagnose why a board-game cover is not rendering.

    Set either @BoardGameId or @BoardGameName and run against Azure SQL.
    This does not modify data.
*/

SET NOCOUNT ON;

DECLARE @BoardGameId bigint = 10015;
DECLARE @BoardGameName varchar(80) = NULL;

IF @BoardGameName IS NULL
BEGIN
    SELECT @BoardGameName = BoardGameName
    FROM bgd.BoardGame
    WHERE ID = @BoardGameId;
END;

SELECT
    RequestedBoardGameId = @BoardGameId,
    RequestedBoardGameName = @BoardGameName;

SELECT
    bg.ID,
    bg.GID,
    bg.Inactive,
    bg.BoardGameName,
    bg.FK_bgd_Club,
    bg.FK_bgd_TemplateBoardGame,
    CoverRowsByOwnerId = COUNT(siOwner.ID),
    CoverRowsByLegacyBlobKey = COUNT(siBlob.ID)
FROM bgd.BoardGame AS bg
LEFT JOIN bgd.StoredImage AS siOwner
    ON siOwner.OwnerType = N'GameCover'
   AND siOwner.OwnerId = bg.ID
LEFT JOIN bgd.StoredImage AS siBlob
    ON siBlob.OwnerType = N'GameCover'
   AND siBlob.BlobKey LIKE CONCAT(N'boardgame/front/', CONVERT(varchar(36), bg.GID), N'.%')
WHERE bg.BoardGameName = @BoardGameName
GROUP BY
    bg.ID,
    bg.GID,
    bg.Inactive,
    bg.BoardGameName,
    bg.FK_bgd_Club,
    bg.FK_bgd_TemplateBoardGame
ORDER BY
    bg.FK_bgd_Club,
    bg.ID;

SELECT
    si.ID,
    si.OwnerType,
    si.OwnerId,
    OwnerBoardGameName = ownerGame.BoardGameName,
    OwnerBoardGameGid = ownerGame.GID,
    si.BlobKey,
    si.PublicUrl,
    si.ContentType,
    si.SizeBytes,
    si.CreatedAtUtc
FROM bgd.StoredImage AS si
LEFT JOIN bgd.BoardGame AS ownerGame
    ON ownerGame.ID = si.OwnerId
WHERE si.OwnerType = N'GameCover'
  AND
  (
      ownerGame.BoardGameName = @BoardGameName
      OR EXISTS
      (
          SELECT 1
          FROM bgd.BoardGame AS sameName
          WHERE sameName.BoardGameName = @BoardGameName
            AND si.BlobKey LIKE CONCAT(N'boardgame/front/', CONVERT(varchar(36), sameName.GID), N'.%')
      )
  )
ORDER BY
    CASE WHEN ownerGame.BoardGameName = @BoardGameName THEN 0 ELSE 1 END,
    si.OwnerId,
    si.BlobKey;
