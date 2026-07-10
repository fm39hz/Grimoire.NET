namespace Grimoire.Application.Export;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;
using Service.Contract;

public class ImageAssetCollector(
	IAssetRepository assetRepository,
	ISegmentRepository segmentRepository,
	IStorageService storageService) {

	public async Task<IReadOnlyDictionary<string, ResolvedAsset>> CollectAsync(
		List<VolumeModel> volumes,
		IReadOnlyDictionary<Guid, List<ChapterModel>> chapterMap,
		CancellationToken cancellationToken = default) {

		var assetKeyToIdMap = await BuildAssetKeyToIdMapAsync(chapterMap, volumes, cancellationToken);
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

	private async Task<Dictionary<string, Guid>> BuildAssetKeyToIdMapAsync(
		IReadOnlyDictionary<Guid, List<ChapterModel>> chapterMap,
		List<VolumeModel> volumes,
		CancellationToken cancellationToken) {

		var chapterPaths = chapterMap.Values
			.SelectMany(chapters => chapters)
			.Select(c => c.Path)
			.ToList();

		var imageSegments = await segmentRepository.FindImageSegmentsByChapterPaths(chapterPaths, cancellationToken);

		var result = imageSegments
			.Select(seg => (seg.AssetKey,
				PrefixedId.TryToGuid(seg.AssetKey, EntityPrefix.Asset, out var id) ? id : Guid.Empty))
			.Where(pair => pair.Item2 != Guid.Empty)
			.DistinctBy(pair => pair.Item2)
			.ToDictionary(pair => pair.AssetKey, pair => pair.Item2);

		foreach (var volume in volumes) {
			var coverKey = volume.Metadata?.CoverImage;
			if (string.IsNullOrEmpty(coverKey)) {
				continue;
			}

			if (!PrefixedId.TryToGuid(coverKey, EntityPrefix.Asset, out var id)) {
				continue;
			}

			if (result.ContainsKey(coverKey)) {
				continue;
			}

			result[coverKey] = id;
		}

		return result;
	}

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
