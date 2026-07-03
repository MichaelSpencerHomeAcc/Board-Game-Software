# Azure SQL Update And Data Import

Use these scripts from the repository root. They do not contain secrets. Pass connection strings at runtime or load them from a local secret store.

## Brand New Azure SQL Database

Use `Initialize-NewAzureDbAndImportData.ps1` when the Azure SQL database is empty and needs the full schema plus old SQL data.

```powershell
$source = "<old SQL database connection string>"
$target = "<Azure SQL connection string>"

.\deploy\scripts\Initialize-NewAzureDbAndImportData.ps1 `
  -SourceConnectionString $source `
  -TargetConnectionString $target
```

This creates the base schema, runs a dry-run data import, rolls back the copied data, and stops before post-import migrations. Review the printed row counts.

To copy data for real and finish the remaining migrations on the same database:

```powershell
$source = "<old SQL database connection string>"
$target = "<Azure SQL connection string>"

.\deploy\scripts\Initialize-NewAzureDbAndImportData.ps1 `
  -SourceConnectionString $source `
  -TargetConnectionString $target `
  -SkipBaseSchema `
  -CommitImport
```

The committed run:

1. Copies legacy SQL data.
2. Runs the club tenancy/player club/board game template migrations.
3. Runs `deploy/sql/AddStoredImages.sql`, which adds later migrations including `StoredImage`.

The legacy SQL copy does not create a default club. After the committed import, run the club backfill if the Azure app needs the old single-club data to appear under a club:

```powershell
$target = "<Azure SQL connection string>"

powershell.exe -ExecutionPolicy Bypass -File .\deploy\scripts\Backfill-AzureClubData.ps1 `
  -TargetConnectionString $target `
  -Commit
```

The club backfill creates or finds `Mike's Clubhouse`, links ASP.NET users and active players to it, copies platform board-game templates into that club, repoints existing game history, and moves shelves to the club.

If you do not need a dry run, run the initializer once with `-CommitImport` and omit `-SkipBaseSchema`.

## Existing Azure SQL Database

Use `Update-AzureDbAndImportData.ps1` when the Azure SQL database already has the application schema and only needs the latest migration plus optional data copy.

```powershell
$target = "<Azure SQL connection string>"

.\deploy\scripts\Update-AzureDbAndImportData.ps1 `
  -TargetConnectionString $target
```

This runs:

```text
deploy/sql/AddStoredImages.sql
```

against the Azure SQL database.

## Existing Database: Update And Dry-Run Data Import

```powershell
$source = "<old SQL database connection string>"
$target = "<Azure SQL connection string>"

.\deploy\scripts\Update-AzureDbAndImportData.ps1 `
  -SourceConnectionString $source `
  -TargetConnectionString $target `
  -ImportData
```

Without `-CommitImport`, the existing data-copy script runs as a dry run.

## Existing Database: Update And Commit Data Import

```powershell
$source = "<old SQL database connection string>"
$target = "<Azure SQL connection string>"

.\deploy\scripts\Update-AzureDbAndImportData.ps1 `
  -SourceConnectionString $source `
  -TargetConnectionString $target `
  -ImportData `
  -CommitImport
```

## Notes

- Run this from the repository root.
- Do not commit real connection strings.
- The import uses `DatabaseScripts/EFRebuild/Copy-DataBetweenSeparateLogins.ps1`.
- The data-copy script copies structured SQL data into an existing target schema.
- Uploaded image bytes are not imported into SQL. Production images should live in Azure Blob Storage, with SQL storing only `StoredImage` metadata.
- Back up the old database before running a committed import.
- Run the dry-run import first and review counts before using `-CommitImport`.
