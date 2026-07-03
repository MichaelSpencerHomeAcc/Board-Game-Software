param(
    [Parameter(Mandatory = $true)]
    [string] $TargetConnectionString,

    [string] $SourceConnectionString,

    [string] $MigrationScriptPath = ".\deploy\sql\AddStoredImages.sql",

    [string] $DataCopyScriptPath = ".\DatabaseScripts\EFRebuild\Copy-DataBetweenSeparateLogins.ps1",

    [switch] $ImportData,

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

$migrationScript = Resolve-RequiredPath $MigrationScriptPath "Migration SQL script"

Write-Host "Updating Azure SQL database with migration script:"
Write-Host "  $migrationScript"
Invoke-SqlScript -connectionString $TargetConnectionString -scriptPath $migrationScript
Write-Host "Azure SQL migration script completed."

if (-not $ImportData) {
    Write-Host "Data import skipped. Pass -ImportData with -SourceConnectionString to copy old database data."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($SourceConnectionString)) {
    throw "SourceConnectionString is required when -ImportData is used."
}

$dataCopyScript = Resolve-RequiredPath $DataCopyScriptPath "Data copy script"

Write-Host "Importing data from old database into Azure SQL target:"
Write-Host "  $dataCopyScript"

$copyArgs = @{
    SourceConnectionString = $SourceConnectionString
    TargetConnectionString = $TargetConnectionString
}

if ($CommitImport) {
    $copyArgs.Commit = $true
}

& $dataCopyScript @copyArgs

if ($CommitImport) {
    Write-Host "Data import committed."
}
else {
    Write-Host "Data import dry run completed. Re-run with -CommitImport to commit copied data."
}
