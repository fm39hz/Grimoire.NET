namespace Grimoire.Infrastructure.Export.Epub;

using Application.Dto.Book;
using Grimoire.Infrastructure.Export.Common;

/// <summary>
///     Processes table of contents section
///     NOTE: Kept for potential future use, but currently TOC processing is handled
///     directly in EpubExportStrategy.ProcessSections to maintain proper NavPoint ordering
/// </summary>
public class TocSectionProcessor : ISectionProcessor<EpubSectionProcessorContext> {
	public Task ProcessAsync(ExportSectionDto section, EpubSectionProcessorContext context) =>
		// Placeholder - actual TOC processing is done in EpubExportStrategy
		// to ensure NavPoint is inserted at correct position before rendering
		Task.CompletedTask;
}
