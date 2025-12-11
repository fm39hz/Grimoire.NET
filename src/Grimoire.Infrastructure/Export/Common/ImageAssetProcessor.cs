namespace Grimoire.Infrastructure.Export.Common;

using Application.Common;
using Application.Service.Contract;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Microsoft.Extensions.Logging;

/// <summary>
///     Handles image asset processing for export strategies
/// </summary>
public class ImageAssetProcessor(
	IAssetService assetService,
	IStorageService storageService,
	ILogger logger) {
	/// <summary>
	///     Processes all images for a list of chapters and returns a mapping of asset keys to file paths
	/// </summary>
	public async Task<Dictionary<string, string>> ProcessChapterImages(
		IEnumerable<ChapterModel> chapters,
		Func<Guid, string, int, string> fileNameGenerator) {
		var imageFileMap = new Dictionary<string, string>();
		var assetToPath = new Dictionary<Guid, string>();

		foreach (var chapter in chapters) {
			if (chapter.ContentData == null) {
				continue;
			}

			var imageSegments = chapter.ContentData.Segments
				.OfType<ImageSegmentModel>()
				.ToList();

			var imageIndex = 1;
			foreach (var imageSegment in imageSegments) {
				if (!PrefixedId.TryToGuid(imageSegment.AssetKey, EntityPrefix.Asset, out var assetId)) {
					logger.LogWarning("Invalid asset ID format: {AssetKey}", imageSegment.AssetKey);
					imageFileMap[imageSegment.AssetKey] = imageSegment.AssetKey;
					continue;
				}

				// Reuse existing path if asset was already processed
				if (assetToPath.TryGetValue(assetId, out var existingPath)) {
					imageFileMap[imageSegment.AssetKey] = existingPath;
					continue;
				}

				var asset = await assetService.FindOne(assetId);
				if (asset == null) {
					logger.LogWarning("Asset {AssetId} not found in repository", assetId);
					continue;
				}

				var extension = Path.GetExtension(asset.Path);
				if (string.IsNullOrEmpty(extension)) {
					extension = ".jpg";
				}

				var relativePath = fileNameGenerator(assetId, extension, imageIndex);
				imageFileMap[imageSegment.AssetKey] = relativePath;
				assetToPath[assetId] = relativePath;

				imageIndex++;
			}
		}

		return imageFileMap;
	}

	/// <summary>
	///     Gets the file stream for an asset
	/// </summary>
	public async Task<Stream?> GetAssetStream(Guid assetId) => await storageService.GetFileStreamAsync(assetId);
}
