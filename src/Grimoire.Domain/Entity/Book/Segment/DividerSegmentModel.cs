namespace Grimoire.Domain.Entity.Book.Segment;

/// <summary>
///     Represents a divider segment within a chapter.
/// </summary>
public sealed class DividerSegmentModel : SegmentModel {
	/// <summary>
	///     Gets or sets the style of the divider.
	/// </summary>
	public string Style { get; set; } = "* * *";
}
