namespace Grimoire.Infrastructure.Export.Epub;

using Application.Dto.Book;
using Common;

/// <summary>
///     Processes table of contents section
///     NOTE: Kept for potential future use, but currently TOC processing is handled
///     directly in EpubExportStrategy.ProcessSections to maintain proper NavPoint ordering
/// </summary>
public class TocSectionProcessor : ISectionProcessor {
	public Task ProcessAsync(ExportSectionDto section, SectionProcessorContext context) {
		// Placeholder - actual TOC processing is done in EpubExportStrategy
		// to ensure NavPoint is inserted at correct position before rendering
		return Task.CompletedTask;
	}
}
