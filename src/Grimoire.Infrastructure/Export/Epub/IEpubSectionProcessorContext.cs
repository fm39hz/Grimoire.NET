namespace Grimoire.Infrastructure.Export.Epub;

using Common;
using Domain.Entity.Book;

/// <summary>
///     Context for EPUB section processing
/// </summary>
public record EpubSectionProcessorContext : IBookSectionProcessorContext {
	public required EpubPackageBuilder PackageBuilder { get; init; }
	public required HtmlRenderer Renderer { get; init; }
	public required SeriesModel Series { get; init; }
	public required List<VolumeModel> Volumes { get; init; }
	public required IReadOnlyDictionary<Guid, List<ChapterModel>> ChapterMap { get; init; }
	public string? Author { get; init; }
	public string? CoverLocalPath { get; init; }
	public Dictionary<string, string>? ImageFileMap { get; init; }
}
