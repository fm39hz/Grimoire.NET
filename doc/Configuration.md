# Configuration Guide

## Storage Configuration

The storage system can be configured through `appsettings.json` or environment variables.

### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Storage:Type` | string | `LocalStorage` | Storage provider type (`LocalStorage` or `S3`) |
| `Storage:BasePath` | string | `grimoire-files` | Base path for file storage |
| `Storage:SeriesPath` | string | `series` | Subdirectory for series files |
| `Storage:UseTemporaryDirectory` | bool | `true` | When true, uses system temp directory as base |

### Configuration via appsettings.json

```json
{
  "Storage": {
    "Type": "LocalStorage",
    "BasePath": "grimoire-files",
    "SeriesPath": "series",
    "UseTemporaryDirectory": true
  }
}
```

### Configuration via Environment Variables

Environment variables use double underscore (`__`) as the section separator:

```bash
# Storage type
export Storage__Type="LocalStorage"

# Base path for storage
export Storage__BasePath="/var/lib/grimoire/files"

# Series subdirectory
export Storage__SeriesPath="series"

# Use temporary directory (true/false)
export Storage__UseTemporaryDirectory="false"
```

### Docker Environment Variables

When using Docker or docker-compose:

```yaml
environment:
  - Storage__Type=LocalStorage
  - Storage__BasePath=/app/data
  - Storage__SeriesPath=series
  - Storage__UseTemporaryDirectory=false
```

### Storage Path Resolution (LocalStorage)

The actual storage path is determined by:

- If `UseTemporaryDirectory` is `true`: `{TempPath}/{BasePath}`
  - Example: `/tmp/grimoire-files/` (Linux) or `C:\Users\...\Temp\grimoire-files\` (Windows)
  
- If `UseTemporaryDirectory` is `false`: `{BasePath}`
  - Example: `/var/lib/grimoire/files/` or `./grimoire-files/`

Files are organized as: `{StoragePath}/{SeriesPath}/{SeriesId}/{FileHash}.{ext}`

---

## S3-Compatible Storage

When `Storage:Type` is set to `S3`, files are stored in an S3-compatible object store.
The SDK used is `AWSSDK.S3` — works with any S3-compatible service.

### S3 Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Storage:S3:Endpoint` | string | `http://localhost:9000` | S3 service endpoint URL |
| `Storage:S3:BucketName` | string | `grimoire` | Bucket name |
| `Storage:S3:AccessKey` | string | — | Access key (store in User Secrets / env var) |
| `Storage:S3:SecretKey` | string | — | Secret key (store in User Secrets / env var) |
| `Storage:S3:UseSsl` | bool | `false` | Enable HTTPS |
| `Storage:S3:Region` | string | `us-east-1` | AWS region / authentication region |

### S3 Object Key Pattern

```
series/{SeriesId}/{FileHash}.{ext}
```

Same structure as LocalStorage, just stored as S3 object keys instead of file paths.

### Configuration via appsettings.json

```json
{
  "Storage": {
    "Type": "S3",
    "S3": {
      "Endpoint": "https://s3.custom-service.com",
      "BucketName": "grimoire",
      "Region": "us-east-1",
      "UseSsl": true
      // AccessKey / SecretKey go in User Secrets or env vars
    }
  }
}
```

### Local Development — User Secrets

```bash
dotnet user-secrets init --project src/Grimoire.Api
dotnet user-secrets set "Storage:S3:AccessKey" "dev-access-key"
dotnet user-secrets set "Storage:S3:SecretKey" "dev-secret-key"
```

### Docker / Production — Environment Variables

```yaml
environment:
  - Storage__Type=S3
  - Storage__S3__Endpoint=https://s3.custom-service.com
  - Storage__S3__BucketName=grimoire
  - Storage__S3__AccessKey=${S3_ACCESS_KEY}
  - Storage__S3__SecretKey=${S3_SECRET_KEY}
  - Storage__S3__Region=us-east-1
  - Storage__S3__UseSsl=true
```

### Important Notes

- `ForcePathStyle = true` is always set — required for non-AWS S3-compatible services.
- `UseSsl` controls the `UseHttp` flag on the AWS SDK config.
- `AccessKey` and `SecretKey` are **never** hardcoded in `appsettings.json`.
  Use User Secrets (local dev) or environment variables (Docker/production).
- Bucket is auto-created on the first file upload if it doesn't exist.

---

### Production Recommendations (LocalStorage)

For production deployments:

1. Set `UseTemporaryDirectory` to `false`
2. Use an absolute path for `BasePath`
3. Ensure the directory has appropriate permissions
4. Consider using a dedicated volume or mount point

Example production configuration:

```json
{
  "Storage": {
    "Type": "LocalStorage",
    "BasePath": "/var/lib/grimoire/storage",
    "SeriesPath": "series",
    "UseTemporaryDirectory": false
  }
}
```

Or via environment variables:

```bash
export Storage__Type="LocalStorage"
export Storage__BasePath="/var/lib/grimoire/storage"
export Storage__SeriesPath="series"
export Storage__UseTemporaryDirectory="false"
```
