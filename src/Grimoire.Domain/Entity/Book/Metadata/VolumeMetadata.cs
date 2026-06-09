namespace Grimoire.Domain.Entity.Book.Metadata;

/// <summary>
///     Represents the strongly-typed metadata for a volume within a series.
///     This is a value object stored as JSONB, not a separate entity.
/// </summary>
public sealed record VolumeMetadata {
	/// <summary>
	///     Gets or sets the URL of the cover image for the volume
	/// </summary>
	public string? CoverImage { get; init; } = string.Empty;

	/// <summary>
	///     Gets or sets the publication date of the volume.
	/// </summary>
	public DateTime? PublicationDate { get; init; }

	/// <summary>
	///     Gets or sets the ISBN of the volume.
	/// </summary>
	public string Isbn { get; init; } = string.Empty;
}
