param(
    [Parameter(Mandatory = $true)]
    [string] $SourceConnectionString,

    [Parameter(Mandatory = $true)]
    [string] $TargetConnectionString,

    [switch] $Commit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$migrationActor = "EF rebuild migration"
$migratedAt = [DateTime]::UtcNow

$tables = @(
    @{ Schema = "dbo"; Table = "AspNetRoles" },
    @{ Schema = "dbo"; Table = "AspNetUsers" },
    @{ Schema = "dbo"; Table = "AspNetRoleClaims" },
    @{ Schema = "dbo"; Table = "AspNetUserClaims" },
    @{ Schema = "dbo"; Table = "AspNetUserLogins" },
    @{ Schema = "dbo"; Table = "AspNetUserRoles" },
    @{ Schema = "dbo"; Table = "AspNetUserTokens" },

    @{ Schema = "bgd"; Table = "BoardGameImageType" },
    @{ Schema = "bgd"; Table = "BoardGameNight" },
    @{ Schema = "bgd"; Table = "BoardGameType" },
    @{ Schema = "bgd"; Table = "BoardGameVictoryConditionType" },
    @{ Schema = "bgd"; Table = "EloMethod" },
    @{ Schema = "bgd"; Table = "MarkerAdditionalType" },
    @{ Schema = "bgd"; Table = "MarkerAlignmentType" },
    @{ Schema = "bgd"; Table = "Player" },
    @{ Schema = "bgd"; Table = "Publisher" },
    @{ Schema = "bgd"; Table = "RankingQueryStore" },
    @{ Schema = "bgd"; Table = "ReleaseVersion" },
    @{ Schema = "bgd"; Table = "ResultType" },
    @{ Schema = "bgd"; Table = "Shelf" },
    @{ Schema = "bgd"; Table = "BoardGameMarkerType" },
    @{ Schema = "bgd"; Table = "BoardGame" },
    @{ Schema = "bgd"; Table = "ShelfSection" },
    @{ Schema = "bgd"; Table = "BoardGameNightPlayer" },
    @{ Schema = "bgd"; Table = "BoardGameEloMethod" },
    @{ Schema = "bgd"; Table = "BoardGameExpansion" },
    @{ Schema = "bgd"; Table = "BoardGameMarker" },
    @{ Schema = "bgd"; Table = "BoardGameMatch" },
    @{ Schema = "bgd"; Table = "BoardGameResult" },
    @{ Schema = "bgd"; Table = "BoardGameVote" },
    @{ Schema = "bgd"; Table = "PlayerBoardGame" },
    @{ Schema = "bgd"; Table = "PlayerBoardGameRating" },
    @{ Schema = "bgd"; Table = "PlayerBoardGameStarRating" },
    @{ Schema = "bgd"; Table = "BoardGameShelfSection" },
    @{ Schema = "bgd"; Table = "BoardGameMatchPlayer" },
    @{ Schema = "bgd"; Table = "BoardGameNightBoardGameMatch" },
    @{ Schema = "bgd"; Table = "PlayerAchievement" },
    @{ Schema = "bgd"; Table = "BoardGameMatchPlayerResult" }
)

function Quote-SqlName([string] $name) {
    return "[" + $name.Replace("]", "]]") + "]"
}

function New-Command([System.Data.SqlClient.SqlConnection] $connection, [System.Data.SqlClient.SqlTransaction] $transaction, [string] $sql) {
    $command = $connection.CreateCommand()
    $command.Transaction = $transaction
    $command.CommandTimeout = 120
    $command.CommandText = $sql
    return $command
}

function Get-TargetColumns(
    [System.Data.SqlClient.SqlConnection] $connection,
    [System.Data.SqlClient.SqlTransaction] $transaction,
    [string] $schema,
    [string] $table
) {
    $sql = @"
SELECT
    c.name,
    c.column_id,
    c.is_identity
FROM sys.columns c
INNER JOIN sys.objects o ON o.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE s.name = @SchemaName
  AND o.name = @TableName
  AND o.type = N'U'
  AND c.is_computed = 0
  AND c.name <> N'VersionStamp'
ORDER BY c.column_id;
"@

    $command = New-Command $connection $transaction $sql
    [void] $command.Parameters.AddWithValue("@SchemaName", $schema)
    [void] $command.Parameters.AddWithValue("@TableName", $table)

    $columns = @()
    $reader = $command.ExecuteReader()
    try {
        while ($reader.Read()) {
            $columns += [pscustomobject]@{
                Name = [string] $reader["name"]
                IsIdentity = [bool] $reader["is_identity"]
            }
        }
    }
    finally {
        $reader.Close()
    }

    return @($columns)
}

function Get-RowCount(
    [System.Data.SqlClient.SqlConnection] $connection,
    [System.Data.SqlClient.SqlTransaction] $transaction,
    [string] $schema,
    [string] $table
) {
    $sql = "SELECT COUNT_BIG(*) FROM $(Quote-SqlName $schema).$(Quote-SqlName $table);"
    $command = New-Command $connection $transaction $sql
    return [long] $command.ExecuteScalar()
}

$sourceConnection = [System.Data.SqlClient.SqlConnection]::new($SourceConnectionString)
$targetConnection = [System.Data.SqlClient.SqlConnection]::new($TargetConnectionString)

$sourceConnection.Open()
$targetConnection.Open()

$sourceTransaction = $sourceConnection.BeginTransaction()
$targetTransaction = $targetConnection.BeginTransaction()

try {
    foreach ($entry in $tables) {
        $schema = $entry.Schema
        $table = $entry.Table
        $targetCount = Get-RowCount $targetConnection $targetTransaction $schema $table

        if ($targetCount -ne 0) {
            throw "Target table $schema.$table is not empty. Stop before copying duplicate data."
        }

        $columns = @(Get-TargetColumns $targetConnection $targetTransaction $schema $table)

        if ($columns.Count -eq 0) {
            throw "No target columns found for $schema.$table."
        }

        $quotedColumns = @()
        $selectColumns = @()

        foreach ($column in $columns) {
            $quotedColumns += Quote-SqlName $column.Name

            switch ($column.Name) {
                "CreatedBy" { $selectColumns += "@MigrationActor AS [CreatedBy]"; break }
                "ModifiedBy" { $selectColumns += "@MigrationActor AS [ModifiedBy]"; break }
                "TimeCreated" { $selectColumns += "@MigratedAt AS [TimeCreated]"; break }
                "TimeModified" { $selectColumns += "@MigratedAt AS [TimeModified]"; break }
                default { $selectColumns += Quote-SqlName $column.Name }
            }
        }

        $columnList = $quotedColumns -join ", "
        $selectList = $selectColumns -join ", "

        $sourceSql = "SELECT $selectList FROM $(Quote-SqlName $schema).$(Quote-SqlName $table);"

        if ($schema -eq "bgd" -and $table -eq "PlayerBoardGame") {
            $sourceSql = @"
WITH RankedTopTen AS
(
    SELECT
        $selectList,
        ROW_NUMBER() OVER (
            PARTITION BY [FK_bgd_Player], [Rank]
            ORDER BY [ID] DESC
        ) AS ActiveRankDuplicateNumber
    FROM [bgd].[PlayerBoardGame]
    WHERE [Inactive] = 0
      AND [FK_bgd_Player] IS NOT NULL
),
AllRows AS
(
    SELECT
        $selectList,
        CAST(0 AS bit) AS ForceInactive
    FROM [bgd].[PlayerBoardGame] source
    WHERE source.[Inactive] = 1
       OR source.[FK_bgd_Player] IS NULL

    UNION ALL

    SELECT
        $selectList,
        CASE WHEN ActiveRankDuplicateNumber = 1 THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS ForceInactive
    FROM RankedTopTen source
)
SELECT
    $(
        ($columns | ForEach-Object {
            if ($_.Name -eq "Inactive") {
                "CASE WHEN ForceInactive = 1 THEN CAST(1 AS bit) ELSE [Inactive] END AS [Inactive]"
            }
            else {
                Quote-SqlName $_.Name
            }
        }) -join ", "
    )
FROM AllRows;
"@
        }
        $sourceCommand = New-Command $sourceConnection $sourceTransaction $sourceSql
        [void] $sourceCommand.Parameters.AddWithValue("@MigrationActor", $migrationActor)
        [void] $sourceCommand.Parameters.AddWithValue("@MigratedAt", $migratedAt)

        $identityColumns = @($columns | Where-Object { $_.IsIdentity })
        $hasIdentity = $identityColumns.Count -gt 0
        $targetObject = "$(Quote-SqlName $schema).$(Quote-SqlName $table)"

        if ($hasIdentity) {
            (New-Command $targetConnection $targetTransaction "SET IDENTITY_INSERT $targetObject ON;").ExecuteNonQuery() | Out-Null
        }

        $reader = $sourceCommand.ExecuteReader()
        try {
            $bulkCopy = [System.Data.SqlClient.SqlBulkCopy]::new(
                $targetConnection,
                [System.Data.SqlClient.SqlBulkCopyOptions]::KeepIdentity,
                $targetTransaction)
            $bulkCopy.DestinationTableName = $targetObject
            $bulkCopy.BulkCopyTimeout = 120

            foreach ($column in $columns) {
                [void] $bulkCopy.ColumnMappings.Add($column.Name, $column.Name)
            }

            $bulkCopy.WriteToServer($reader)
        }
        finally {
            $reader.Close()
        }

        if ($hasIdentity) {
            (New-Command $targetConnection $targetTransaction "SET IDENTITY_INSERT $targetObject OFF;").ExecuteNonQuery() | Out-Null
        }

        $sourceCount = Get-RowCount $sourceConnection $sourceTransaction $schema $table
        $copiedCount = Get-RowCount $targetConnection $targetTransaction $schema $table
        Write-Host ("{0}.{1}: source={2}, target={3}" -f $schema, $table, $sourceCount, $copiedCount)
    }

    if ($Commit) {
        $targetTransaction.Commit()
        $sourceTransaction.Commit()
        Write-Host "Copy committed."
    }
    else {
        $targetTransaction.Rollback()
        $sourceTransaction.Rollback()
        Write-Host "Copy rolled back. Re-run with -Commit after reviewing counts."
    }
}
catch {
    $targetTransaction.Rollback()
    $sourceTransaction.Rollback()
    throw
}
finally {
    $targetConnection.Dispose()
    $sourceConnection.Dispose()
}
