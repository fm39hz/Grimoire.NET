namespace Grimoire.Application.Export;

using System.Threading;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Service.Contract;

public class ImageAssetCollector(IAssetRepository assetRepository, IStorageService storageService) {
	public async Task<IReadOnlyDictionary<string, ResolvedAsset>> CollectAsync(
		IReadOnlyDictionary<Guid, List<ChapterModel>> chapterMap,
		CancellationToken cancellationToken = default) {
		var assetKeyToIdMap = BuildAssetKeyToIdMap(chapterMap);
		if (assetKeyToIdMap.Count == 0) {
			return new Dictionary<string, ResolvedAsset>();
		}

		var assets = await assetRepository.FindByIdsAsync(assetKeyToIdMap.Values, cancellationToken);

		var result = new Dictionary<string, ResolvedAsset>();
		foreach (var entry in assetKeyToIdMap) {
			if (!assets.TryGetValue(entry.Value, out var asset)) {
				continue;
			}

			var capturedId = entry.Value;
			if (!await StreamExistsAsync(capturedId, cancellationToken)) {
				continue;
			}

			result[entry.Key] = new ResolvedAsset(
				asset,
				async () => (await storageService.GetFileStreamAsync(capturedId, cancellationToken))?.Stream);
		}

		return result;
	}

	public static IReadOnlyDictionary<string, string> GenerateFileMap(
		IReadOnlyDictionary<string, ResolvedAsset> imageAssets) {
		var index = 1;
		var result = new Dictionary<string, string>();
		foreach (var (assetKey, resolved) in imageAssets) {
			var fileName = resolved.Asset.OriginalFileName;
			result[assetKey] = BuildExportFileName(fileName, index);
			index++;
		}
		return result;
	}

	public static string BuildExportFileName(string originalFileName, int index) {
		var ext = Path.GetExtension(originalFileName);
		var name = Path.GetFileNameWithoutExtension(originalFileName);
		var sanitized = string.Join("_",
			name.ToLowerInvariant()
				.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
		return $"{sanitized}_{index:D3}{ext}";
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

	private async Task<bool> StreamExistsAsync(Guid assetId, CancellationToken cancellationToken) {
		try {
			var result = await storageService.GetFileStreamAsync(assetId, cancellationToken);
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
