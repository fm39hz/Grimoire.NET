namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Represents a text segment within a chapter, composed of multiple text runs.
/// </summary>
public sealed record TextSegment : Segment {
	/// <summary>
	///     Gets or sets the list of text runs that make up this segment.
	/// </summary>
	public ICollection<TextRun> Runs { get; init; } = [];
}
