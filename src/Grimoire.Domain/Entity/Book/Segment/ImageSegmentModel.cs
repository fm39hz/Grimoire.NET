namespace Grimoire.Domain.Entity.Book.Segment;

/// <summary>
///     Represents an image segment within a chapter.
/// </summary>
public sealed record ImageSegmentModel : SegmentModel {
	/// <summary>
	///     Gets or sets the asset key (S3 path) for the image.
	/// </summary>
	public required string AssetKey { get; init; }

	/// <summary>
	///     Gets or sets the optional caption for the image.
	/// </summary>
	public string? Caption { get; init; }
}
