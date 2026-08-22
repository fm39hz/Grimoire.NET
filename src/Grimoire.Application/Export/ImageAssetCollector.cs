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
using Microsoft.Extensions.Logging;
using Service.Contract;

public partial class ImageAssetCollector(
	IAssetRepository assetRepository,
	ISegmentRepository segmentRepository,
	IStorageService storageService,
	ILogger<ImageAssetCollector> logger) {

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
				LogAssetMissing(entry.Key, entry.Value);
				continue;
			}

			var capturedId = entry.Value;
			if (!await AssetStreamProbe.IsReadableAsync(storageService, capturedId, cancellationToken)) {
				LogStreamUnreadable(entry.Key, capturedId);
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
				LogInvalidCoverKey(volume.Id, coverKey);
				continue;
			}

			if (result.ContainsKey(coverKey)) {
				continue;
			}

			result[coverKey] = id;
		}

		return result;
	}

	[LoggerMessage(LogLevel.Warning, "Export asset {AssetKey} (id {AssetId}) was not found in the repository and will be omitted from the package.")]
	partial void LogAssetMissing(string assetKey, Guid assetId);

	[LoggerMessage(LogLevel.Warning, "Export asset {AssetKey} (id {AssetId}) has an empty or unreadable backing stream and will be omitted from the package.")]
	partial void LogStreamUnreadable(string assetKey, Guid assetId);

	[LoggerMessage(LogLevel.Warning, "Volume {VolumeId} references a cover image with an invalid asset key '{CoverKey}'; cover will be omitted for this volume.")]
	partial void LogInvalidCoverKey(Guid volumeId, string coverKey);
}
