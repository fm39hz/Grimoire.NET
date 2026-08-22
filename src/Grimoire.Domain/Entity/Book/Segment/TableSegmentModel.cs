namespace Grimoire.Domain.Entity.Book.Segment;

/// <summary>
///     A formatted table cell composed of inline text runs.
/// </summary>
public sealed record TableCell(IReadOnlyList<TextRun> Runs);

/// <summary>
///     Represents a GFM-style table segment within a chapter.
/// </summary>
public sealed class TableSegmentModel : SegmentModel {
	/// <summary>
	///     Header cells. A table must have at least one header cell.
	/// </summary>
	public List<TableCell> Header { get; set; } = [];

	/// <summary>
	///     Body rows; every row must have the same number of cells as <see cref="Header"/>.
	/// </summary>
	public List<List<TableCell>> Rows { get; set; } = [];
}
