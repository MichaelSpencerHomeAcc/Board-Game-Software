/*
    Create Mike's Clubhouse board-game collection from platform templates,
    then repoint existing play/history data to those club-owned games.

    Run after:
    - 004_add_club_tenancy_foundation.sql
    - 005_add_player_club_memberships.sql
    - 006_add_boardgame_club_templates.sql

    What this does:
    - Finds bgd.Club by @ClubName.
    - Copies every global bgd.BoardGame template into that club if it is not already copied.
    - Copies per-game config rows: results, markers, ELO methods, and expansion links.
    - Assigns old unassigned game nights to the club.
    - Repoints existing matches/votes/player game rows/ratings to the club copies.

    Existing global templates are left intact for Platform Admin mode.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ClubName nvarchar(120);
DECLARE @Actor nvarchar(128);
DECLARE @Now datetime;
DECLARE @ClubId bigint;

SET @ClubName = N'Mike''s Clubhouse';
SET @Actor = N'EF club board-game backfill';
SET @Now = GETUTCDATE();
SET @ClubId = NULL;

SELECT @ClubId = c.ID
FROM bgd.Club AS c
WHERE c.ClubName = @ClubName
  AND c.Inactive = 0;

IF @ClubId IS NULL
BEGIN
    THROW 51000, 'Could not find active club named Mike''s Clubhouse.', 1;
END;

BEGIN TRANSACTION;

DECLARE @GameMap TABLE
(
    TemplateBoardGameId bigint NOT NULL PRIMARY KEY,
    ClubBoardGameId bigint NOT NULL
);

INSERT INTO @GameMap (TemplateBoardGameId, ClubBoardGameId)
SELECT template.ID, clubGame.ID
FROM bgd.BoardGame AS template
JOIN bgd.BoardGame AS clubGame
    ON clubGame.FK_bgd_TemplateBoardGame = template.ID
   AND clubGame.FK_bgd_Club = @ClubId
WHERE template.FK_bgd_Club IS NULL;

MERGE bgd.BoardGame AS target
USING
(
    SELECT
        ID,
        Inactive,
        BoardGameName,
        FK_bgd_BoardGameType,
        FK_bgd_BoardGameVictoryConditionType,
        FK_bgd_Publisher,
        PlayerCountMin,
        PlayerCountMax,
        PlayingTimeMinInMinutes,
        PlayingTimeMaxInMinutes,
        ComplexityRating,
        ReleaseDate,
        HasMarkers,
        IsExpansion,
        HeightCm,
        WidthCm,
        BoardGameSummary,
        HowToPlayHyperlink
    FROM bgd.BoardGame
    WHERE FK_bgd_Club IS NULL
) AS source
ON 1 = 0
WHEN NOT MATCHED BY TARGET
     AND NOT EXISTS
     (
         SELECT 1
         FROM bgd.BoardGame AS existing
         WHERE existing.FK_bgd_Club = @ClubId
           AND existing.FK_bgd_TemplateBoardGame = source.ID
     )
THEN INSERT
(
    GID,
    Inactive,
    CreatedBy,
    TimeCreated,
    ModifiedBy,
    TimeModified,
    BoardGameName,
    FK_bgd_BoardGameType,
    FK_bgd_BoardGameVictoryConditionType,
    FK_bgd_Publisher,
    FK_bgd_Club,
    FK_bgd_TemplateBoardGame,
    PlayerCountMin,
    PlayerCountMax,
    PlayingTimeMinInMinutes,
    PlayingTimeMaxInMinutes,
    ComplexityRating,
    ReleaseDate,
    HasMarkers,
    IsExpansion,
    HeightCm,
    WidthCm,
    BoardGameSummary,
    HowToPlayHyperlink
)
VALUES
(
    NEWID(),
    source.Inactive,
    @Actor,
    @Now,
    @Actor,
    @Now,
    source.BoardGameName,
    source.FK_bgd_BoardGameType,
    source.FK_bgd_BoardGameVictoryConditionType,
    source.FK_bgd_Publisher,
    @ClubId,
    source.ID,
    source.PlayerCountMin,
    source.PlayerCountMax,
    source.PlayingTimeMinInMinutes,
    source.PlayingTimeMaxInMinutes,
    source.ComplexityRating,
    source.ReleaseDate,
    source.HasMarkers,
    source.IsExpansion,
    source.HeightCm,
    source.WidthCm,
    source.BoardGameSummary,
    source.HowToPlayHyperlink
)
OUTPUT source.ID, inserted.ID
INTO @GameMap (TemplateBoardGameId, ClubBoardGameId);

INSERT INTO bgd.BoardGameResult
(
    GID,
    Inactive,
    CreatedBy,
    TimeCreated,
    ModifiedBy,
    TimeModified,
    FK_bgd_BoardGame,
    FK_bgd_ResultType
)
SELECT
    NEWID(),
    r.Inactive,
    @Actor,
    @Now,
    @Actor,
    @Now,
    m.ClubBoardGameId,
    r.FK_bgd_ResultType
FROM bgd.BoardGameResult AS r
JOIN @GameMap AS m ON m.TemplateBoardGameId = r.FK_bgd_BoardGame
WHERE NOT EXISTS
(
    SELECT 1
    FROM bgd.BoardGameResult AS existing
    WHERE existing.FK_bgd_BoardGame = m.ClubBoardGameId
      AND existing.FK_bgd_ResultType = r.FK_bgd_ResultType
);

INSERT INTO bgd.BoardGameMarker
(
    GID,
    Inactive,
    CreatedBy,
    TimeCreated,
    ModifiedBy,
    TimeModified,
    FK_bgd_BoardGame,
    FK_bgd_BoardGameMarkerType
)
SELECT
    NEWID(),
    marker.Inactive,
    @Actor,
    @Now,
    @Actor,
    @Now,
    m.ClubBoardGameId,
    marker.FK_bgd_BoardGameMarkerType
FROM bgd.BoardGameMarker AS marker
JOIN @GameMap AS m ON m.TemplateBoardGameId = marker.FK_bgd_BoardGame
WHERE NOT EXISTS
(
    SELECT 1
    FROM bgd.BoardGameMarker AS existing
    WHERE existing.FK_bgd_BoardGame = m.ClubBoardGameId
      AND
      (
          existing.FK_bgd_BoardGameMarkerType = marker.FK_bgd_BoardGameMarkerType
          OR (existing.FK_bgd_BoardGameMarkerType IS NULL AND marker.FK_bgd_BoardGameMarkerType IS NULL)
      )
);

INSERT INTO bgd.BoardGameEloMethod
(
    GID,
    Inactive,
    CreatedBy,
    TimeCreated,
    ModifiedBy,
    TimeModified,
    FK_bgd_BoardGame,
    FK_bgd_EloMethod,
    ExpectedWinRatioTeamA,
    Notes
)
SELECT
    NEWID(),
    elo.Inactive,
    @Actor,
    @Now,
    @Actor,
    @Now,
    m.ClubBoardGameId,
    elo.FK_bgd_EloMethod,
    elo.ExpectedWinRatioTeamA,
    elo.Notes
FROM bgd.BoardGameEloMethod AS elo
JOIN @GameMap AS m ON m.TemplateBoardGameId = elo.FK_bgd_BoardGame
WHERE NOT EXISTS
(
    SELECT 1
    FROM bgd.BoardGameEloMethod AS existing
    WHERE existing.FK_bgd_BoardGame = m.ClubBoardGameId
      AND existing.FK_bgd_EloMethod = elo.FK_bgd_EloMethod
);

INSERT INTO bgd.BoardGameExpansion
(
    GID,
    Inactive,
    CreatedBy,
    TimeCreated,
    ModifiedBy,
    TimeModified,
    FK_bgd_BoardGame,
    FK_bgd_ExpansionBoardGame
)
SELECT
    NEWID(),
    expansion.Inactive,
    @Actor,
    @Now,
    @Actor,
    @Now,
    baseMap.ClubBoardGameId,
    expansionMap.ClubBoardGameId
FROM bgd.BoardGameExpansion AS expansion
JOIN @GameMap AS baseMap ON baseMap.TemplateBoardGameId = expansion.FK_bgd_BoardGame
JOIN @GameMap AS expansionMap ON expansionMap.TemplateBoardGameId = expansion.FK_bgd_ExpansionBoardGame
WHERE NOT EXISTS
(
    SELECT 1
    FROM bgd.BoardGameExpansion AS existing
    WHERE existing.FK_bgd_BoardGame = baseMap.ClubBoardGameId
      AND existing.FK_bgd_ExpansionBoardGame = expansionMap.ClubBoardGameId
);

UPDATE night
SET
    night.FK_bgd_Club = @ClubId,
    night.ModifiedBy = @Actor,
    night.TimeModified = @Now
FROM bgd.BoardGameNight AS night
WHERE night.FK_bgd_Club IS NULL;

UPDATE match
SET
    match.FK_bgd_BoardGame = m.ClubBoardGameId,
    match.ModifiedBy = @Actor,
    match.TimeModified = @Now
FROM bgd.BoardGameMatch AS match
JOIN @GameMap AS m ON m.TemplateBoardGameId = match.FK_bgd_BoardGame;

UPDATE vote
SET
    vote.FK_bgd_BoardGame = m.ClubBoardGameId,
    vote.ModifiedBy = @Actor,
    vote.TimeModified = @Now
FROM bgd.BoardGameVote AS vote
JOIN @GameMap AS m ON m.TemplateBoardGameId = vote.FK_bgd_BoardGame
WHERE NOT EXISTS
(
    SELECT 1
    FROM bgd.BoardGameVote AS existing
    WHERE existing.FK_bgd_BoardGameNight = vote.FK_bgd_BoardGameNight
      AND existing.FK_bgd_BoardGame = m.ClubBoardGameId
      AND existing.FK_bgd_Player = vote.FK_bgd_Player
);

UPDATE pbg
SET
    pbg.FK_bgd_BoardGame = m.ClubBoardGameId,
    pbg.ModifiedBy = @Actor,
    pbg.TimeModified = @Now
FROM bgd.PlayerBoardGame AS pbg
JOIN @GameMap AS m ON m.TemplateBoardGameId = pbg.FK_bgd_BoardGame;

UPDATE rating
SET
    rating.FK_bgd_BoardGame = m.ClubBoardGameId,
    rating.ModifiedBy = @Actor,
    rating.TimeModified = @Now
FROM bgd.PlayerBoardGameRating AS rating
JOIN @GameMap AS m ON m.TemplateBoardGameId = rating.FK_bgd_BoardGame
WHERE NOT EXISTS
(
    SELECT 1
    FROM bgd.PlayerBoardGameRating AS existing
    WHERE existing.FK_bgd_Player = rating.FK_bgd_Player
      AND existing.FK_bgd_BoardGame = m.ClubBoardGameId
);

UPDATE stars
SET
    stars.FK_bgd_BoardGame = m.ClubBoardGameId,
    stars.ModifiedBy = @Actor,
    stars.TimeModified = @Now
FROM bgd.PlayerBoardGameStarRating AS stars
JOIN @GameMap AS m ON m.TemplateBoardGameId = stars.FK_bgd_BoardGame
WHERE NOT EXISTS
(
    SELECT 1
    FROM bgd.PlayerBoardGameStarRating AS existing
    WHERE existing.FK_bgd_Player = stars.FK_bgd_Player
      AND existing.FK_bgd_BoardGame = m.ClubBoardGameId
);

UPDATE matchPlayer
SET
    matchPlayer.FK_bgd_BoardGameMarker = clubMarker.ID,
    matchPlayer.ModifiedBy = @Actor,
    matchPlayer.TimeModified = @Now
FROM bgd.BoardGameMatchPlayer AS matchPlayer
JOIN bgd.BoardGameMarker AS templateMarker
    ON templateMarker.ID = matchPlayer.FK_bgd_BoardGameMarker
JOIN @GameMap AS m
    ON m.TemplateBoardGameId = templateMarker.FK_bgd_BoardGame
JOIN bgd.BoardGameMarker AS clubMarker
    ON clubMarker.FK_bgd_BoardGame = m.ClubBoardGameId
   AND
   (
       clubMarker.FK_bgd_BoardGameMarkerType = templateMarker.FK_bgd_BoardGameMarkerType
       OR (clubMarker.FK_bgd_BoardGameMarkerType IS NULL AND templateMarker.FK_bgd_BoardGameMarkerType IS NULL)
   );

SELECT
    @ClubId AS ClubId,
    @ClubName AS ClubName,
    COUNT(*) AS TemplateGamesMapped
FROM @GameMap;

COMMIT;
