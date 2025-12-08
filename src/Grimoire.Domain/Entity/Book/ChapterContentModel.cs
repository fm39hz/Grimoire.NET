namespace Grimoire.Domain.Entity.Book;

using Segment;

/// <summary>
///     Represents the content data for a chapter (vertical partitioning for heavy data)
/// </summary>
public record ChapterContentModel {
	/// <summary>
	///     Primary key and foreign key to Chapter
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Processed content segments (stored as JSONB)
	/// </summary>
	public List<SegmentModel> Segments { get; set; } = [];

	/// <summary>
	///     Footnotes (stored as JSONB)
	/// </summary>
	public List<FootnoteSegmentModel> Footnotes { get; set; } = [];

	/// <summary>
	///     Navigation property to Chapter
	/// </summary>
	public ChapterModel? Chapter { get; set; }
}
