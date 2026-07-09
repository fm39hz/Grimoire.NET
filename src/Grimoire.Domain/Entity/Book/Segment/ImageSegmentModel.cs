namespace Grimoire.Domain.Entity.Book.Segment;

/// <summary>
///     Represents an image segment within a chapter.
/// </summary>
public sealed class ImageSegmentModel : SegmentModel {
	/// <summary>
	///     Gets or sets the asset key (S3 path) for the image.
	/// </summary>
	public required string AssetKey { get; set; }

	/// <summary>
	///     Gets or sets the optional caption for the image.
	/// </summary>
	public string? Caption { get; set; }
}
