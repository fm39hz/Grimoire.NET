namespace Grimoire.Infrastructure.Export.Epub;

using Domain.Entity.Book;

/// <summary>
///     Interface for rendering different segment types
/// </summary>
public interface ISegmentRenderer {
	/// <summary>
	///     Determines if this renderer can handle the given segment type
	/// </summary>
	public bool CanRender(SegmentModel segment);

	/// <summary>
	///     Renders the segment to HTML
	/// </summary>
	public string Render(SegmentModel segment, SegmentRenderContext context);
}
