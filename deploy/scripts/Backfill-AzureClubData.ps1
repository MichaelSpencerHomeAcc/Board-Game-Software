param(
    [Parameter(Mandatory = $true)]
    [string] $TargetConnectionString,

    [string] $ClubName = "Mike's Clubhouse",

    [string] $ClubSlug = "mikes-clubhouse",

    [string] $ClubDescription = "Primary migrated club",

    [string] $EfRebuildPath = ".\DatabaseScripts\EFRebuild",

    [switch] $Commit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RequiredPath([string] $path, [string] $description) {
    $resolved = Resolve-Path -Path $path -ErrorAction SilentlyContinue
    if ($null -eq $resolved) {
        throw "$description not found: $path"
    }

    return $resolved.Path
}

function Split-SqlBatches([string] $scriptText) {
    $batches = New-Object System.Collections.Generic.List[string]
    $current = New-Object System.Text.StringBuilder

    foreach ($line in ($scriptText -split "\r?\n")) {
        if ($line -match "^\s*GO\s*(?:--.*)?$") {
            $batch = $current.ToString().Trim()
            if ($batch.Length -gt 0) {
                $batches.Add($batch)
            }

            [void] $current.Clear()
            continue
        }

        [void] $current.AppendLine($line)
    }

    $lastBatch = $current.ToString().Trim()
    if ($lastBatch.Length -gt 0) {
        $batches.Add($lastBatch)
    }

    return @($batches)
}

function Invoke-SqlText([string] $connectionString, [string] $scriptText, [string] $description) {
    Add-Type -AssemblyName System.Data

    $batches = Split-SqlBatches $scriptText
    $connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
    $connection.Open()

    try {
        $index = 0
        foreach ($batch in $batches) {
            $index++
            $command = $connection.CreateCommand()
            $command.CommandTimeout = 300
            $command.CommandText = $batch

            try {
                [void] $command.ExecuteNonQuery()
            }
            catch {
                throw "Failed executing SQL batch $index for $description. $($_.Exception.Message)"
            }
            finally {
                $command.Dispose()
            }
        }
    }
    finally {
        $connection.Dispose()
    }
}

function Invoke-SqlScript([string] $connectionString, [string] $scriptPath) {
    Write-Host "Running $scriptPath"
    Invoke-SqlText -connectionString $connectionString -scriptText (Get-Content -Path $scriptPath -Raw) -description $scriptPath
}

function Escape-SqlLiteral([string] $value) {
    return $value.Replace("'", "''")
}

$efRebuild = Resolve-RequiredPath $EfRebuildPath "EF rebuild script folder"
$boardGameBackfill = Resolve-RequiredPath (Join-Path $efRebuild "007_create_mikes_clubhouse_boardgames_and_repoint_history.sql") "Board-game club backfill script"
$shelfBackfill = Resolve-RequiredPath (Join-Path $efRebuild "008_shelves_to_mikes_clubhouse.sql") "Shelf club backfill script"

$clubNameSql = Escape-SqlLiteral $ClubName
$clubSlugSql = Escape-SqlLiteral $ClubSlug
$clubDescriptionSql = Escape-SqlLiteral $ClubDescription
$actorSql = "Azure club data backfill"

$seedClubSql = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ClubName nvarchar(120) = N'$clubNameSql';
DECLARE @Slug varchar(80) = '$clubSlugSql';
DECLARE @Description nvarchar(500) = N'$clubDescriptionSql';
DECLARE @Actor nvarchar(128) = N'$actorSql';
DECLARE @Now datetime = GETUTCDATE();

BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT 1
    FROM bgd.Club
    WHERE ClubName = @ClubName
      AND Inactive = 0
)
BEGIN
    INSERT INTO bgd.Club
    (
        GID,
        Inactive,
        CreatedBy,
        TimeCreated,
        ModifiedBy,
        TimeModified,
        ClubName,
        Description,
        Slug
    )
    VALUES
    (
        NEWID(),
        0,
        @Actor,
        @Now,
        @Actor,
        @Now,
        @ClubName,
        @Description,
        @Slug
    );
END;

UPDATE bgd.Club
SET Inactive = 0,
    Description = COALESCE(NULLIF(Description, N''), @Description),
    Slug = COALESCE(NULLIF(Slug, ''), @Slug),
    ModifiedBy = @Actor,
    TimeModified = @Now
WHERE ClubName = @ClubName;

SELECT ID AS ClubId, ClubName, Slug
FROM bgd.Club
WHERE ClubName = @ClubName
  AND Inactive = 0;

COMMIT TRANSACTION;
"@

$linkUsersSql = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ClubName nvarchar(120) = N'$clubNameSql';
DECLARE @Actor nvarchar(128) = N'$actorSql';
DECLARE @Now datetime = GETUTCDATE();
DECLARE @ClubId bigint;

SELECT @ClubId = ID
FROM bgd.Club
WHERE ClubName = @ClubName
  AND Inactive = 0;

IF @ClubId IS NULL
BEGIN
    THROW 51000, 'Expected active club was not found.', 1;
END;

BEGIN TRANSACTION;

UPDATE cm
SET Inactive = 0,
    ModifiedBy = @Actor,
    TimeModified = @Now,
    JoinedAt = CASE WHEN cm.JoinedAt IS NULL THEN @Now ELSE cm.JoinedAt END
FROM bgd.ClubMembership AS cm
INNER JOIN dbo.AspNetUsers AS u
    ON u.Id = cm.UserId
WHERE cm.FK_bgd_Club = @ClubId
  AND cm.Inactive = 1;

PRINT CONCAT('Inactive user club memberships reactivated: ', @@ROWCOUNT);

INSERT INTO bgd.ClubMembership
(
    GID,
    Inactive,
    CreatedBy,
    TimeCreated,
    ModifiedBy,
    TimeModified,
    FK_bgd_Club,
    UserId,
    Role,
    JoinedAt
)
SELECT
    NEWID(),
    0,
    @Actor,
    @Now,
    @Actor,
    @Now,
    @ClubId,
    u.Id,
    CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.AspNetUserRoles AS ur
        INNER JOIN dbo.AspNetRoles AS r
            ON r.Id = ur.RoleId
        WHERE ur.UserId = u.Id
          AND r.Name = N'Admin'
    ) THEN 'Owner' ELSE 'Member' END,
    @Now
FROM dbo.AspNetUsers AS u
WHERE NOT EXISTS
(
    SELECT 1
    FROM bgd.ClubMembership AS cm
    WHERE cm.FK_bgd_Club = @ClubId
      AND cm.UserId = u.Id
);

PRINT CONCAT('Missing user club memberships inserted: ', @@ROWCOUNT);

SELECT
    ClubId = @ClubId,
    ActiveUsers = COUNT_BIG(*),
    ActiveUserMemberships = SUM(CASE WHEN cm.ID IS NOT NULL AND cm.Inactive = 0 THEN 1 ELSE 0 END)
FROM dbo.AspNetUsers AS u
LEFT JOIN bgd.ClubMembership AS cm
    ON cm.UserId = u.Id
   AND cm.FK_bgd_Club = @ClubId;

COMMIT TRANSACTION;
"@

$linkPlayersSql = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ClubName nvarchar(120) = N'$clubNameSql';
DECLARE @Actor nvarchar(128) = N'$actorSql';
DECLARE @Now datetime = GETUTCDATE();
DECLARE @ClubId bigint;

SELECT @ClubId = ID
FROM bgd.Club
WHERE ClubName = @ClubName
  AND Inactive = 0;

IF @ClubId IS NULL
BEGIN
    THROW 51000, 'Expected active club was not found.', 1;
END;

BEGIN TRANSACTION;

UPDATE p
SET FK_bgd_Club = @ClubId,
    ModifiedBy = @Actor,
    TimeModified = @Now
FROM bgd.Player AS p
WHERE p.Inactive = 0
  AND p.FK_bgd_Club IS NULL;

PRINT CONCAT('Legacy Player.FK_bgd_Club values set: ', @@ROWCOUNT);

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

PRINT CONCAT('Inactive player club links reactivated: ', @@ROWCOUNT);

INSERT INTO bgd.PlayerClub
(
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
  AND NOT EXISTS
  (
      SELECT 1
      FROM bgd.PlayerClub AS pc
      WHERE pc.FK_bgd_Player = p.ID
        AND pc.FK_bgd_Club = @ClubId
  );

PRINT CONCAT('Missing player club links inserted: ', @@ROWCOUNT);

SELECT
    ClubId = @ClubId,
    ActivePlayers = COUNT_BIG(*),
    ActivePlayersLinkedToClub = SUM(CASE WHEN pc.ID IS NOT NULL AND pc.Inactive = 0 THEN 1 ELSE 0 END),
    ActivePlayersMissingClub = SUM(CASE WHEN pc.ID IS NULL OR pc.Inactive = 1 THEN 1 ELSE 0 END),
    ActivePlayersWithLegacyClub = SUM(CASE WHEN p.FK_bgd_Club = @ClubId THEN 1 ELSE 0 END)
FROM bgd.Player AS p
LEFT JOIN bgd.PlayerClub AS pc
    ON pc.FK_bgd_Player = p.ID
   AND pc.FK_bgd_Club = @ClubId
WHERE p.Inactive = 0;

COMMIT TRANSACTION;
"@

if (-not $Commit) {
    Write-Host "Dry run only. This script will not change Azure SQL unless you pass -Commit."
    Write-Host "Would seed/find club '$ClubName', link users, copy board games, repoint history, move shelves, and link players."
    exit 0
}

Write-Host "Seeding/finding club '$ClubName'."
Invoke-SqlText -connectionString $TargetConnectionString -scriptText $seedClubSql -description "seed club"

Write-Host "Linking ASP.NET users to '$ClubName'."
Invoke-SqlText -connectionString $TargetConnectionString -scriptText $linkUsersSql -description "link club users"

Write-Host "Copying board-game templates into '$ClubName' and repointing play history."
$boardGameScriptText = Get-Content -Path $boardGameBackfill -Raw
$boardGameScriptText = $boardGameScriptText.Replace("SET @ClubName = N'Mike''s Clubhouse';", "SET @ClubName = N'$clubNameSql';")
Invoke-SqlText -connectionString $TargetConnectionString -scriptText $boardGameScriptText -description "board-game club backfill"

Write-Host "Moving shelves into '$ClubName' and repointing shelf links."
$shelfScriptText = Get-Content -Path $shelfBackfill -Raw
$shelfScriptText = $shelfScriptText.Replace("DECLARE @ClubName nvarchar(120) = N'Mike''s Clubhouse';", "DECLARE @ClubName nvarchar(120) = N'$clubNameSql';")
$shelfScriptText = $shelfScriptText.Replace("DECLARE @Commit bit = 0;", "DECLARE @Commit bit = 1;")
Invoke-SqlText -connectionString $TargetConnectionString -scriptText $shelfScriptText -description "shelf club backfill"

Write-Host "Linking active players to '$ClubName'."
Invoke-SqlText -connectionString $TargetConnectionString -scriptText $linkPlayersSql -description "link club players"

Write-Host "Azure club data backfill completed."
