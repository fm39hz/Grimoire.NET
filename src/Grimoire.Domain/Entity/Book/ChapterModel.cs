namespace Grimoire.Domain.Entity.Book;

using Segment;

/// <summary>
///     Represents a chapter within a volume
/// </summary>
public record ChapterModel : BaseModel {
	/// <summary>
	///     Foreign key to the volume
	/// </summary>
	public required Guid VolumeId { get; init; }

	/// <summary>
	///     Order of this chapter within the volume
	/// </summary>
	public float Order { get; set; }

	/// <summary>
	///     Title of the chapter
	/// </summary>
	public required string Title { get; set; }

	/// <summary>
	///     Content of the chapter, composed of various segments.
	/// </summary>
	public List<SegmentModel> Content { get; set; } = [];

	/// <summary>
	///     A dictionary of footnotes, where the key is the footnote ID.
	/// </summary>
	public List<FootnoteSegmentModel> Footnotes { get; set; } = [];
}
