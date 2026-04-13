namespace Grimoire.Application.Export;

using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Dto.Book;
using Service.Contract;

/// <summary>
///     Assembles all data required for any export format.
///     Format strategies must NOT depend on domain services — they receive a BookExportContext instead.
/// </summary>
public class BookExportOrchestrator(
	IVolumeRepository volumeRepository,
	IChapterRepository chapterRepository,
	IAssetRepository assetRepository,
	IAssetService assetService,
	IStorageService storageService) {
	public async Task<BookExportContext> BuildContextAsync(
		SeriesModel series,
		BinderyRequestDto request) {
		var volumes = await ResolveVolumes(series.Id, request);
		var chapterMap = await LoadAllChapters(volumes);
		var (coverAsset, coverStream) = await ResolveCover(series);
		var imageAssets = await CollectImageAssets(chapterMap);

		var assetFileMap = GenerateAssetFileMap(imageAssets);
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

	private static Dictionary<string, string> GenerateAssetFileMap(
		IReadOnlyDictionary<string, ResolvedAsset> imageAssets) {
		var assetFileMap = new Dictionary<string, string>();
		var index = 1;

		foreach (var (assetKey, resolved) in imageAssets) {
			var ext = Path.GetExtension(resolved.Asset.Path);
			if (string.IsNullOrEmpty(ext)) {
				ext = ".jpg";
			}

			var relativePath = $"img{index:D3}{ext}";
			assetFileMap[assetKey] = relativePath;
			index++;
		}

		return assetFileMap;
	}

	private static string? FlattenDescription(List<TextSegmentModel>? description) =>
		description == null || description.Count == 0
			? null
			: string.Join(" ", description.SelectMany(d => d.Runs.Select(r => r.Text)));

	private async Task<List<VolumeModel>> ResolveVolumes(Guid seriesId, BinderyRequestDto request) {
		var allVolumes = await volumeRepository.FindBySeriesId(seriesId);
		var ordered = allVolumes.OrderBy(v => v.Order).ToList();

		if (request.Mode.Equals("Single", StringComparison.OrdinalIgnoreCase)
			&& request.TargetVolumeIds is { Count: > 0 }) {
			var targetSet = request.TargetVolumeIds.ToHashSet();
			ordered = ordered
				.Where(v => targetSet.Contains(PrefixedId.ToString(EntityPrefix.Volume, v.Id)))
				.ToList();
		}

		return ordered;
	}

	private async Task<IReadOnlyDictionary<Guid, List<ChapterModel>>> LoadAllChapters(List<VolumeModel> volumes) {
		if (volumes.Count == 0) {
			return new Dictionary<Guid, List<ChapterModel>>();
		}

		var volumeIds = volumes.Select(v => v.Id).ToList();
		var allChapters = await chapterRepository.FindByVolumeIdsWithContent(volumeIds);

		return allChapters
			.GroupBy(c => c.VolumeId)
			.ToDictionary(g => g.Key, g => g.ToList());
	}

	private async Task<(AssetModel? asset, Func<Task<Stream?>>? streamProvider)> ResolveCover(
		SeriesModel series) {
		var coverKey = series.Metadata?.CoverImage;
		if (string.IsNullOrEmpty(coverKey)) {
			return (null, null);
		}

		if (!PrefixedId.TryToGuid(coverKey, EntityPrefix.Asset, out var id)) {
			return (null, null);
		}

		var asset = await assetService.FindOne(id);
		if (asset == null) {
			return (null, null);
		}

		try {
			var testStream = await storageService.GetFileStreamAsync(id);
			if (testStream == null) {
				return (null, null);
			}

			await testStream.DisposeAsync();
		}
		catch {
			return (null, null);
		}

		return (asset, async () => await storageService.GetFileStreamAsync(id));
	}

	private async Task<IReadOnlyDictionary<string, ResolvedAsset>> CollectImageAssets(
		IReadOnlyDictionary<Guid, List<ChapterModel>> chapterMap) {
		var assetKeyToIdMap = chapterMap.Values
			.SelectMany(chapters => chapters)
			.SelectMany(chapter => chapter.ContentData!.Segments.OfType<ImageSegmentModel>())
			.Select(seg => (seg.AssetKey,
				PrefixedId.TryToGuid(seg.AssetKey, EntityPrefix.Asset, out var id) ? id : Guid.Empty))
			.Where(pair => pair.Item2 != Guid.Empty)
			.DistinctBy(pair => pair.Item2)
			.ToDictionary(pair => pair.AssetKey, pair => pair.Item2);

		if (assetKeyToIdMap.Count == 0) {
			return new Dictionary<string, ResolvedAsset>();
		}

		var assets = await assetRepository.FindByIdsAsync(assetKeyToIdMap.Values);

		var result = new Dictionary<string, ResolvedAsset>();
		foreach (var entry in assetKeyToIdMap) {
			if (!assets.TryGetValue(entry.Value, out var asset)) {
				continue;
			}

			var capturedId = entry.Value;

			// Validate that image file exists and can be opened
			try {
				var testStream = await storageService.GetFileStreamAsync(capturedId);
				if (testStream == null) {
					continue;
				}

				await testStream.DisposeAsync();
			}
			catch {
				continue;
			}

			result[entry.Key] = new ResolvedAsset(
				asset,
				async () => await storageService.GetFileStreamAsync(capturedId));
		}

		return result;
	}
}
