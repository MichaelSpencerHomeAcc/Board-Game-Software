# Mongo image blob exporter

Exports `BoardGameImages` documents from MongoDB into local blob-style files plus `manifest.json`.

## Output layout

Examples:

```text
artifacts/image-blobs/
  boardgame/front/{gid}.jpg
  player/{gid}.png
  publisher/{gid}.png
  marker-type/{gid}.webp
  misc/{sql-table}/{gid}/{image-type-or-source-id}.jpg
  manifest.json
  summary.json
```

`manifest.json` contains the source Mongo `_id`, table name, `GID`, `ImageTypeGID`, content type, file size, SHA-256 hash, and focus metadata.

## Run

Build once:

```powershell
dotnet build .\Tools\MongoImageBlobExporter\MongoImageBlobExporter.csproj
```

Run using environment variables:

```powershell
$env:MONGODB_CONNECTION_STRING = "<your mongo connection string>"
$env:MONGODB_DATABASE = "<your mongo database>"
dotnet .\Tools\MongoImageBlobExporter\bin\Debug\net8.0\MongoImageBlobExporter.dll --output .\artifacts\image-blobs
```

Or pass values directly:

```powershell
dotnet .\Tools\MongoImageBlobExporter\bin\Debug\net8.0\MongoImageBlobExporter.dll `
  --connection-string "<your mongo connection string>" `
  --database "<your mongo database>" `
  --output .\artifacts\image-blobs
```

Test with a small sample first:

```powershell
dotnet .\Tools\MongoImageBlobExporter\bin\Debug\net8.0\MongoImageBlobExporter.dll `
  --connection-string "<your mongo connection string>" `
  --database "<your mongo database>" `
  --output .\artifacts\image-blobs-test `
  --limit 10
```

Use `--overwrite` to replace existing exported files.

Do not commit exported files. The default `artifacts/` folder is already ignored by git.
