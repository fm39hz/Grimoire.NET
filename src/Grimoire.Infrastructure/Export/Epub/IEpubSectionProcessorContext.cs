namespace Grimoire.Infrastructure.Export.Epub;

using Grimoire.Domain.Entity.Book;
using Grimoire.Infrastructure.Export.Common;

/// <summary>
///     Context for EPUB section processing
/// </summary>
public record EpubSectionProcessorContext : IBookSectionProcessorContext {
	public required SeriesModel Series { get; init; }
	public required List<VolumeModel> Volumes { get; init; }
	public required IReadOnlyDictionary<Guid, List<ChapterModel>> ChapterMap { get; init; }
	public string? Author { get; init; }
	public string? CoverLocalPath { get; init; }
	public Dictionary<string, string>? ImageFileMap { get; init; }

	public required EpubPackageBuilder PackageBuilder { get; init; }
	public required HtmlRenderer Renderer { get; init; }
}
