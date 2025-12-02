namespace Grimoire.Domain.Entity.Book.Segment;

/// <summary>
///     Represents a text segment within a chapter, composed of multiple text runs.
/// </summary>
public sealed record TextSegmentModel : SegmentModel {
	/// <summary>
	///     Gets or sets the list of text runs that make up this segment.
	/// </summary>
	public ICollection<TextRun> Runs { get; init; } = [];
}
