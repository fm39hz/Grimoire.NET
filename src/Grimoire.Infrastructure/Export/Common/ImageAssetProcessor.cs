namespace Grimoire.Infrastructure.Export.Common;

using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;

/// <summary>
///     Handles image asset processing for export strategies
/// </summary>
public class ImageAssetProcessor(IAssetRepository assetRepository, IStorageRepository storageRepository) {
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
					imageFileMap[imageSegment.AssetKey] = imageSegment.AssetKey;
					continue;
				}

				// Reuse existing path if asset was already processed
				if (assetToPath.TryGetValue(assetId, out var existingPath)) {
					imageFileMap[imageSegment.AssetKey] = existingPath;
					continue;
				}

				var asset = await assetRepository.FindOne(assetId);
				if (asset == null) {
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
	public async Task<Stream?> GetAssetStream(Guid assetId) => await storageRepository.GetFileStreamAsync(assetId);
}
