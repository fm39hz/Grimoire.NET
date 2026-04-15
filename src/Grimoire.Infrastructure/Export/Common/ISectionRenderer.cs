namespace Grimoire.Infrastructure.Export.Common;

using Application.Dto.Book;
using Application.Export;
using Application.Service.Strategy;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

public interface ISectionRenderer {
	public ExportFormat Format { get; }

	/// <summary>
	/// Maps a domain section to a render action.
	/// Returns NavEntries to be added to the book's navigation tree.
	/// </summary>
	public IReadOnlyList<NavEntry> RenderSection(
		BookExportContext context,
		ExportSectionDto section,
		IPackageBuilder builder);

	/// <summary>
	/// Renders a list of segments to the format string (HTML or Markdown).
	/// </summary>
	public string RenderSegments(
		IEnumerable<SegmentModel> segments,
		List<FootnoteSegmentModel>? footnotes = null,
		IReadOnlyDictionary<string, string>? assetMap = null);

	/// <summary>
	/// Renders description text segments to the format string.
	/// </summary>
	public string RenderDescription(
		IEnumerable<TextSegmentModel> segments,
		IReadOnlyDictionary<string, string>? assetMap = null);
}
