/*
    Link all active players to Club ID 1.

    This is safe to rerun:
    - Existing active PlayerClub links are left alone.
    - Existing inactive PlayerClub links for Club 1 are reactivated.
    - Missing links are inserted.
    - Legacy Player.FK_bgd_Club is set to Club 1 where it is still blank.

    Run with @Commit = 0 first. If the printed counts look right, set @Commit = 1.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ClubId bigint = 1;
DECLARE @Actor nvarchar(128) = N'club player backfill';
DECLARE @Now datetime = GETUTCDATE();
DECLARE @Commit bit = 0;

IF OBJECT_ID(N'bgd.PlayerClub', N'U') IS NULL
BEGIN
    THROW 51000, 'bgd.PlayerClub does not exist. Run the player-club EF migration first.', 1;
END;

IF NOT EXISTS (
    SELECT 1
    FROM bgd.Club
    WHERE ID = @ClubId
      AND Inactive = 0
)
BEGIN
    THROW 51001, 'Club ID 1 does not exist or is inactive.', 1;
END;

BEGIN TRANSACTION;

IF COL_LENGTH('bgd.Player', 'FK_bgd_Club') IS NOT NULL
BEGIN
    UPDATE p
    SET FK_bgd_Club = @ClubId,
        ModifiedBy = @Actor,
        TimeModified = @Now
    FROM bgd.Player AS p
    WHERE p.Inactive = 0
      AND p.FK_bgd_Club IS NULL;

    PRINT CONCAT('Legacy Player.FK_bgd_Club values set to Club 1: ', @@ROWCOUNT);
END;

UPDATE pc
SET Inactive = 0,
    ModifiedBy = @Actor,
    TimeModified = @Now,
    JoinedAt = CASE WHEN pc.JoinedAt IS NULL THEN @Now ELSE pc.JoinedAt END
FROM bgd.PlayerClub AS pc
INNER JOIN bgd.Player AS p
    ON p.ID = pc.FK_bgd_Player
WHERE pc.FK_bgd_Club = @ClubId
  AND pc.Inactive = 1
  AND p.Inactive = 0;

PRINT CONCAT('Inactive Club 1 player links reactivated: ', @@ROWCOUNT);

INSERT INTO bgd.PlayerClub (
    GID,
    Inactive,
    CreatedBy,
    TimeCreated,
    ModifiedBy,
    TimeModified,
    FK_bgd_Player,
    FK_bgd_Club,
    JoinedAt
)
SELECT
    NEWID(),
    0,
    @Actor,
    @Now,
    @Actor,
    @Now,
    p.ID,
    @ClubId,
    @Now
FROM bgd.Player AS p
WHERE p.Inactive = 0
  AND NOT EXISTS (
      SELECT 1
      FROM bgd.PlayerClub AS pc
      WHERE pc.FK_bgd_Player = p.ID
        AND pc.FK_bgd_Club = @ClubId
  );

PRINT CONCAT('Missing Club 1 player links inserted: ', @@ROWCOUNT);

SELECT
    ActivePlayers = COUNT_BIG(*),
    ActivePlayersLinkedToClub1 = SUM(CASE WHEN pc.ID IS NOT NULL AND pc.Inactive = 0 THEN 1 ELSE 0 END),
    ActivePlayersMissingClub1 = SUM(CASE WHEN pc.ID IS NULL OR pc.Inactive = 1 THEN 1 ELSE 0 END),
    ActivePlayersWithLegacyClub1 = SUM(CASE WHEN p.FK_bgd_Club = @ClubId THEN 1 ELSE 0 END),
    ActivePlayersMissingLegacyClub1 = SUM(CASE WHEN p.FK_bgd_Club IS NULL THEN 1 ELSE 0 END)
FROM bgd.Player AS p
LEFT JOIN bgd.PlayerClub AS pc
    ON pc.FK_bgd_Player = p.ID
   AND pc.FK_bgd_Club = @ClubId
WHERE p.Inactive = 0;

IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT 'Committed Club 1 player backfill.';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'Rolled back. Set @Commit = 1 after reviewing the counts.';
END;
