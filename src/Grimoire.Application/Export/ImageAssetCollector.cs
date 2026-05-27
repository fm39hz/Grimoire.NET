namespace Grimoire.Application.Export;

using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Service.Contract;

public class ImageAssetCollector(IAssetRepository assetRepository, IStorageService storageService) {
	public async Task<IReadOnlyDictionary<string, ResolvedAsset>> CollectAsync(
		IReadOnlyDictionary<Guid, List<ChapterModel>> chapterMap) {
		var assetKeyToIdMap = BuildAssetKeyToIdMap(chapterMap);
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
			if (!await StreamExistsAsync(capturedId)) {
				continue;
			}

			result[entry.Key] = new ResolvedAsset(
				asset,
				async () => (await storageService.GetFileStreamAsync(capturedId))?.Stream);
		}

		return result;
	}

	public static IReadOnlyDictionary<string, string> GenerateFileMap(
		IReadOnlyDictionary<string, ResolvedAsset> imageAssets) {
		var assetFileMap = new Dictionary<string, string>();
		var index = 1;

		foreach (var (assetKey, resolved) in imageAssets) {
			var ext = Path.GetExtension(resolved.Asset.Path);
			if (string.IsNullOrEmpty(ext)) {
				ext = ".jpg";
			}

			assetFileMap[assetKey] = $"img{index:D3}{ext}";
			index++;
		}

		return assetFileMap;
	}

	private static Dictionary<string, Guid> BuildAssetKeyToIdMap(
		IReadOnlyDictionary<Guid, List<ChapterModel>> chapterMap) => chapterMap.Values
			.SelectMany(chapters => chapters)
			.SelectMany(chapter => chapter.ContentData!.Segments.OfType<ImageSegmentModel>())
			.Select(seg => (seg.AssetKey,
				PrefixedId.TryToGuid(seg.AssetKey, EntityPrefix.Asset, out var id) ? id : Guid.Empty))
			.Where(pair => pair.Item2 != Guid.Empty)
			.DistinctBy(pair => pair.Item2)
			.ToDictionary(pair => pair.AssetKey, pair => pair.Item2);

	private async Task<bool> StreamExistsAsync(Guid assetId) {
		try {
			var result = await storageService.GetFileStreamAsync(assetId);
			if (result == null) {
				return false;
			}

			await result.Stream.DisposeAsync();
			return true;
		}
		catch {
			return false;
		}
	}
}
