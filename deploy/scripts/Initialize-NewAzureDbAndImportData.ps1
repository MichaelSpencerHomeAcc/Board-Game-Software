param(
    [Parameter(Mandatory = $true)]
    [string] $SourceConnectionString,

    [Parameter(Mandatory = $true)]
    [string] $TargetConnectionString,

    [string] $EfRebuildPath = ".\DatabaseScripts\EFRebuild",

    [string] $LatestMigrationScriptPath = ".\deploy\sql\AddStoredImages.sql",

    [switch] $SkipBaseSchema,

    [switch] $CommitImport
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

function Invoke-SqlScript([string] $connectionString, [string] $scriptPath) {
    Add-Type -AssemblyName System.Data

    $scriptText = Get-Content -Path $scriptPath -Raw
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
                throw "Failed executing SQL batch $index from $scriptPath. $($_.Exception.Message)"
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

function Invoke-NamedSqlScript([string] $efRebuildRoot, [string] $fileName, [string] $connectionString) {
    $scriptPath = Resolve-RequiredPath (Join-Path $efRebuildRoot $fileName) "SQL script"
    Write-Host "Running $fileName"
    Invoke-SqlScript -connectionString $connectionString -scriptPath $scriptPath
}

$efRebuild = Resolve-RequiredPath $EfRebuildPath "EF rebuild script folder"
$latestMigrationScript = Resolve-RequiredPath $LatestMigrationScriptPath "Latest migration SQL script"
$dataCopyScript = Resolve-RequiredPath (Join-Path $efRebuild "Copy-DataBetweenSeparateLogins.ps1") "Data copy script"

if (-not $SkipBaseSchema) {
    Write-Host "Creating base schema in the new Azure SQL database."
    Invoke-NamedSqlScript $efRebuild "001_identity_migrations_idempotent.sql" $TargetConnectionString
    Invoke-NamedSqlScript $efRebuild "002_boardgame_model_create.sql" $TargetConnectionString
    Invoke-NamedSqlScript $efRebuild "002b_boardgame_mark_migrations_applied.sql" $TargetConnectionString
    Invoke-NamedSqlScript $efRebuild "002d_fix_active_top_ten_unique_index.sql" $TargetConnectionString
    Invoke-NamedSqlScript $efRebuild "002c_functional_objects_from_old_db.sql" $TargetConnectionString
}
else {
    Write-Host "Skipping base schema creation."
}

Write-Host "Copying legacy SQL data into Azure SQL."
$copyArgs = @{
    SourceConnectionString = $SourceConnectionString
    TargetConnectionString = $TargetConnectionString
}

if ($CommitImport) {
    $copyArgs.Commit = $true
}

& $dataCopyScript @copyArgs

if (-not $CommitImport) {
    Write-Host "Dry-run import completed and rolled back."
    Write-Host "The database is left at the pre-import base schema stage."
    Write-Host "After reviewing row counts, rerun with -SkipBaseSchema -CommitImport to copy data for real and finish migrations."
    exit 0
}

Write-Host "Applying post-import migrations."
Invoke-NamedSqlScript $efRebuild "004_add_club_tenancy_foundation.sql" $TargetConnectionString
Invoke-NamedSqlScript $efRebuild "005_add_player_club_memberships.sql" $TargetConnectionString
Invoke-NamedSqlScript $efRebuild "006_add_boardgame_club_templates.sql" $TargetConnectionString

Write-Host "Applying latest idempotent EF migration script."
Invoke-SqlScript -connectionString $TargetConnectionString -scriptPath $latestMigrationScript

Write-Host "New Azure SQL database initialization and data import completed."
Write-Host "Optional one-off scripts under DatabaseScripts\EFRebuild, such as 008 and 009, are not run automatically."
