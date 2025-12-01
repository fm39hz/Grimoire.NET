namespace Grimoire.Domain.Entity.Book;

/// <summary>
///     Represents a divider segment within a chapter.
/// </summary>
public sealed record DividerSegment : Segment {
	/// <summary>
	///     Gets or sets the style of the divider.
	/// </summary>
	public string Style { get; init; } = "* * *";
}
