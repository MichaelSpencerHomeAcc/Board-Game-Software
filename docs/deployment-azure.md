# Azure Deployment

This app is deployed with an Azure-only production model:

- Azure App Service hosts the ASP.NET Core app.
- Azure SQL Database stores structured application data.
- Azure Blob Storage stores uploaded images.
- GitHub Actions publishes the app to Azure App Service.

Do not commit production connection strings, publish profiles, storage keys, or user-uploaded image files.

## Project Audit

- .NET version: `net8.0`
- Startup file: `Program.cs`
- Identity DbContext: `ApplicationDbContextMain`
- Application DbContext: `BoardGameDbContext`
- Database provider: `Microsoft.EntityFrameworkCore.SqlServer`
- Connection source: `ConnectionStrings:DefaultConnection`
- Image metadata table: `bgd.StoredImage`
- Image upload service: `ImageService`
- Blob abstraction: `IBlobStorageService`
- Blob implementation: `AzureBlobStorageService`
- Runtime image rendering: `StoredImage.PublicUrl` or `/media/...` redirects to stored public URLs

Runtime image storage must not use `wwwroot/uploads`, MongoDB image documents, SQL image byte arrays, or base64 image fields. The legacy `Tools/MongoImageBlobExporter` project is an offline migration utility and is excluded from the web app project.

## Azure Resource Names

Recommended production names:

- Resource group: `rg-boardgameclub-prod`
- App Service plan: `asp-boardgameclub-prod`
- App Service: `app-boardgameclub-prod`
- Azure SQL logical server: `sql-boardgameclub-prod`
- Azure SQL database: `sqldb-boardgameclub-prod`
- Storage account: `stboardgameclubprod`
- Blob container: `images`

Storage account names must be globally unique, so adjust `stboardgameclubprod` if Azure reports that it is unavailable.

## Create App Service

1. Create a resource group, for example `rg-boardgameclub-prod`.
2. Create an App Service plan for Linux or Windows. The GitHub workflow builds a framework-dependent .NET 8 app.
3. Create the App Service named `app-boardgameclub-prod`.
4. Set the stack/runtime to .NET 8.
5. In App Service configuration, add the environment variables listed below.
6. Enable HTTPS only.
7. Set the health check path to `/health` if using App Service health checks.

## Create Azure SQL Database

1. Create an Azure SQL logical server.
2. Create the production database, for example `sqldb-boardgameclub-prod`.
3. Configure firewall/network access so the App Service can connect.
4. Copy the ADO.NET connection string.
5. Store it in App Service configuration as `ConnectionStrings__DefaultConnection`.

Do not put the production SQL connection string in `appsettings.json`.

## Create Blob Storage

1. Create a Storage Account.
2. Create a Blob container named `images`.
3. Set the container access level according to the chosen public image strategy. The current app stores public image URLs and the upload service creates the container with blob-level public access if it does not exist.
4. Copy the storage account connection string.
5. Set `AzureBlob__PublicBaseUrl` to the public container URL, for example:

```text
https://stboardgameclubprod.blob.core.windows.net/images
```

## App Service Environment Variables

Set these in Azure App Service Configuration:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<Azure SQL connection string>
Storage__Provider=AzureBlob
AzureBlob__ConnectionString=<Storage account connection string>
AzureBlob__ContainerName=images
AzureBlob__PublicBaseUrl=https://<storage-account>.blob.core.windows.net/images
ImageUploads__MaxSizeBytes=5242880
```

Optional bootstrap admin setting:

```text
Identity__BootstrapAdminEmail=<admin email>
```

## Run Migrations

For early production, generate and manually run the idempotent SQL script against Azure SQL:

```powershell
dotnet ef migrations script --context BoardGameDbContext --idempotent -o deploy/sql/AddStoredImages.sql
```

The generated script is:

```text
deploy/sql/AddStoredImages.sql
```

Run it against the Azure SQL production database using Azure Data Studio, SQL Server Management Studio, `sqlcmd`, or the Azure Portal query editor.

## Manual Deploy

To create a local Release publish:

```powershell
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet publish --configuration Release --no-build --output ./publish
```

Deploy the `publish` output to `app-boardgameclub-prod` using Azure App Service deployment tools or the Azure Portal.

## GitHub Actions Deployment

The production workflow is:

```text
.github/workflows/deploy-azure-production.yml
```

It runs on:

- Push to `main`
- Manual `workflow_dispatch`

The workflow restores, builds, tests, publishes, and deploys with:

```text
azure/webapps-deploy@v3
```

Create this GitHub repository secret:

```text
AZURE_WEBAPP_PUBLISH_PROFILE
```

To get the value:

1. Open Azure App Service `app-boardgameclub-prod`.
2. Go to Overview.
3. Download the publish profile.
4. Copy the file contents into the GitHub secret.
5. Delete the downloaded publish profile file from your machine when finished.

Do not commit the publish profile file.

## Health Endpoint

The app exposes:

```text
/health
```

The health endpoint checks:

- The app can resolve its configuration.
- SQL connection string is present.
- SQL connectivity succeeds.
- Azure Blob configuration is present.

The health endpoint does not upload, delete, or mutate Blob Storage.

## Image Storage Behavior

Uploaded images are validated before upload:

- Allowed extensions: `.jpg`, `.jpeg`, `.png`, `.webp`
- Allowed MIME types: `image/jpeg`, `image/png`, `image/webp`
- Maximum size: 5 MB by default
- Server-generated blob keys are used
- Original filenames are stored only as metadata

Blob key patterns:

```text
clubs/{clubId}/logo/{guid}{ext}
games/{gameId}/cover/{guid}{ext}
users/{userId}/avatar/{guid}{ext}
game-nights/{gameNightId}/photos/{guid}{ext}
```

SQL stores only image metadata in `bgd.StoredImage`. Image bytes live in Azure Blob Storage.
