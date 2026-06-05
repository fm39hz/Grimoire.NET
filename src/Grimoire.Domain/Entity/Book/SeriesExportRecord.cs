namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Tracks the last export for a series per format.
///     Used for dedup: if source content hasn't changed since last export, skip rebuilding the file.
/// </summary>
public class SeriesExportRecord : BaseModel
{
    public Guid SeriesId { get; init; }
    public SeriesModel? Series { get; init; }

    /// <summary>Export format (e.g., "epub", "pdf", "markdown").</summary>
    public required string Format { get; init; }

    /// <summary>When this export was last performed.</summary>
    public DateTime LastExportedAt { get; set; }

    /// <summary>The asset ID of the last exported file.</summary>
    public Guid AssetId { get; set; }
    public AssetModel? Asset { get; init; }
}
