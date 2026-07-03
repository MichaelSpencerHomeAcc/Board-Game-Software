# Mongo image blob importer

Imports files created by `Tools/MongoImageBlobExporter` into Azure Blob Storage and writes `bgd.StoredImage` metadata rows to Azure SQL.

This is an offline migration tool. It is excluded from the production web app project.

## Prerequisites

1. The Azure SQL database has already been created and imported from the old SQL database.
2. `bgd.StoredImage` exists in Azure SQL.
3. The Mongo image exporter has produced:

```text
artifacts/image-blobs/manifest.json
artifacts/image-blobs/**
```

## Build

```powershell
dotnet build .\Tools\MongoImageBlobImporter\MongoImageBlobImporter.csproj
```

## Dry Run

Use a dry run first. It checks that each manifest row can be mapped to an imported SQL owner row and that each local file exists. It does not upload blobs or insert SQL metadata.

```powershell
$env:AZURE_SQL_CONNECTION_STRING = "<Azure SQL connection string>"
$env:AZURE_BLOB_CONNECTION_STRING = "<Azure Storage connection string>"
$env:AZURE_BLOB_PUBLIC_BASE_URL = "https://stboardgameclubprod.blob.core.windows.net/images"

dotnet .\Tools\MongoImageBlobImporter\bin\Debug\net8.0\MongoImageBlobImporter.dll `
  --manifest .\artifacts\image-blobs\manifest.json `
  --dry-run
```

## Import

```powershell
$env:AZURE_SQL_CONNECTION_STRING = "<Azure SQL connection string>"
$env:AZURE_BLOB_CONNECTION_STRING = "<Azure Storage connection string>"
$env:AZURE_BLOB_PUBLIC_BASE_URL = "https://stboardgameclubprod.blob.core.windows.net/images"

dotnet .\Tools\MongoImageBlobImporter\bin\Debug\net8.0\MongoImageBlobImporter.dll `
  --manifest .\artifacts\image-blobs\manifest.json
```

## What It Imports

The importer supports these old Mongo image owners:

- `bgd.BoardGame` / `BoardGames` -> `GameCover`
- `bgd.Player` -> `UserAvatar`
- `bgd.Publisher` / `Publishers` -> `PublisherLogo`
- `bgd.BoardGameMarkerType` -> `MarkerTypeImage`

Unsupported `misc/...` manifest rows are skipped and reported.

## Notes

- Do not commit exported image files.
- Do not commit connection strings.
- Re-run with `--replace-metadata` only if you intentionally want to replace existing `StoredImage` rows for matching blob keys.
- The importer preserves the exporter blob keys, such as `boardgame/front/{gid}.jpg` and `player/{gid}.png`; the app renders through `StoredImage.PublicUrl`, so these keys do not need to match new upload keys.
