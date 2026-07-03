# Azure SQL Update And Data Import

Use `Update-AzureDbAndImportData.ps1` from the repository root to update the Azure SQL schema and optionally copy data from the old SQL database.

The script does not contain secrets. Pass connection strings at runtime or load them from a local secret store.

## Update Azure SQL Schema Only

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

## Update Azure SQL And Dry-Run Data Import

```powershell
$source = "<old SQL database connection string>"
$target = "<Azure SQL connection string>"

.\deploy\scripts\Update-AzureDbAndImportData.ps1 `
  -SourceConnectionString $source `
  -TargetConnectionString $target `
  -ImportData
```

Without `-CommitImport`, the existing data-copy script runs as a dry run.

## Update Azure SQL And Commit Data Import

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
