namespace Grimoire.Infrastructure.Export.Epub;

using Application.Dto.Book;
using Domain.Entity.Book;

/// <summary>
///     Processes a specific section type for EPUB export
/// </summary>
public interface ISectionProcessor {
	/// <summary>
	///     Processes the section and adds it to the package builder
	/// </summary>
	public Task ProcessAsync(
		ExportSectionDto section,
		SectionProcessorContext context);
}

/// <summary>
///     Context for section processing
/// </summary>
public class SectionProcessorContext {
	public required SeriesModel Series { get; init; }
	public required List<VolumeModel> Volumes { get; init; }
	public required EpubPackageBuilder PackageBuilder { get; init; }
	public required HtmlRenderer Renderer { get; init; }
	public string? CoverLocalPath { get; init; }
	public Dictionary<string, string>? ImageFileMap { get; init; }
	public string? Author { get; init; }
}
