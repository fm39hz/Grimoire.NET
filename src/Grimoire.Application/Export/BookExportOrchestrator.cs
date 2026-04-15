namespace Grimoire.Application.Export;

using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;

/// <summary>
///     Assembles all data required for any export format.
///     Format strategies must NOT depend on domain services — they receive a BookExportContext instead.
/// </summary>
public class BookExportOrchestrator(
	VolumeResolver volumeResolver,
	ChapterLoader chapterLoader,
	CoverResolver coverResolver,
	ImageAssetCollector imageAssetCollector) {

	public async Task<BookExportContext> BuildContextAsync(
		SeriesModel series,
		BinderyRequestDto request) {
		var volumes = await volumeResolver.ResolveAsync(series.Id, request);
		var chapterMap = await chapterLoader.LoadAsync(volumes);
		var (coverAsset, coverStream) = await coverResolver.ResolveAsync(series);
		var imageAssets = await imageAssetCollector.CollectAsync(chapterMap);
		var assetFileMap = ImageAssetCollector.GenerateFileMap(imageAssets);
		var plainTextDescription = FlattenDescription(series.Metadata?.Description);

		return new BookExportContext {
			Series = series,
			Volumes = volumes,
			ChapterMap = chapterMap,
			CoverAsset = coverAsset,
			CoverStreamProvider = coverStream,
			ImageAssets = imageAssets,
			AssetFileMap = assetFileMap,
			PlainTextDescription = plainTextDescription,
			Structure = request.Structure
		};
	}

	private static string? FlattenDescription(List<TextSegmentModel>? description) =>
		description == null || description.Count == 0
			? null
			: string.Join(" ", description.SelectMany(d => d.Runs.Select(r => r.Text)));
}
