namespace Grimoire.Domain.Entity.Book;

using Metadata;

/// <summary>
///     Represents a volume within a series
/// </summary>
public record VolumeModel : BaseModel {
	/// <summary>
	///     Foreign key to the series
	/// </summary>
	public required Guid SeriesId { get; init; }

	/// <summary>
	///     Order of this volume within the series
	/// </summary>
	public float Order { get; set; }

	/// <summary>
	///     Title of the volume
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     Strongly-typed metadata for the volume
	/// </summary>
	public VolumeMetadata? Metadata { get; set; }
}
