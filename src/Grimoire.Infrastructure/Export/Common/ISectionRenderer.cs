namespace Grimoire.Infrastructure.Export.Common;

using Application.Dto.Book;
using Application.Export;
using Application.Service.Strategy;

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
}
