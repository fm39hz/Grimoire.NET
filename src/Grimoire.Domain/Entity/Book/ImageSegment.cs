namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Represents an image segment within a chapter.
/// </summary>
public sealed record ImageSegment : Segment {
	/// <summary>
	///     Gets or sets the asset key (MinIO path) for the image.
	/// </summary>
	public required string AssetKey { get; init; }

	/// <summary>
	///     Gets or sets the optional caption for the image.
	/// </summary>
	public string? Caption { get; init; }
}
