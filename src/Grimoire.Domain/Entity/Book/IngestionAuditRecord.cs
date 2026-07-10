namespace Grimoire.Domain.Entity.Book;

using System;

/// <summary>
///     Represents an audit log entry for a book ingestion process.
/// </summary>
public class IngestionAuditRecord : BaseModel {
	/// <summary>The ID of the series being ingested.</summary>
	public Guid SeriesId { get; init; }
	public SeriesModel? Series { get; init; }

	/// <summary>The source type: e.g. "Obsidian", "EPUB", "Markdown".</summary>
	public required string SourceType { get; init; }

	/// <summary>The status of the ingestion: "Pending", "Success", "Failed".</summary>
	public required string Status { get; set; }

	/// <summary>Error message if the ingestion failed.</summary>
	public string? ErrorMessage { get; set; }

	/// <summary>Optional JSON summary of ingestion metrics (e.g. segments modified, asset mappings).</summary>
	public string? Summary { get; set; }

	/// <summary>When the ingestion started.</summary>
	public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

	/// <summary>When the ingestion finished (if completed).</summary>
	public DateTimeOffset? CompletedAt { get; set; }
}
