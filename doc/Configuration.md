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

### Storage Path Resolution

The actual storage path is determined by:

- If `UseTemporaryDirectory` is `true`: `{TempPath}/{BasePath}`
  - Example: `/tmp/grimoire-files/` (Linux) or `C:\Users\...\Temp\grimoire-files\` (Windows)
  
- If `UseTemporaryDirectory` is `false`: `{BasePath}`
  - Example: `/var/lib/grimoire/files/` or `./grimoire-files/`

Files are organized as: `{StoragePath}/{SeriesPath}/{SeriesId}/{FileHash}.{ext}`

### Production Recommendations

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
