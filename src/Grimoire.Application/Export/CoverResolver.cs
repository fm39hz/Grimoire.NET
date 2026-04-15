namespace Grimoire.Application.Export;

using Domain.Common;
using Domain.Entity.Book;
using Service.Contract;

public class CoverResolver(IAssetService assetService, IStorageService storageService) {
	public async Task<(AssetModel? asset, Func<Task<Stream?>>? streamProvider)> ResolveAsync(
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

		// TODO: Replace stream probe with IStorageRepository.ExistsAsync(id)
		// once the storage layer interface supports it.
		if (!await StreamExistsAsync(id)) {
			return (null, null);
		}

		return (asset, async () => await storageService.GetFileStreamAsync(id));
	}

	private async Task<bool> StreamExistsAsync(Guid assetId) {
		try {
			var stream = await storageService.GetFileStreamAsync(assetId);
			if (stream == null) {
				return false;
			}

			await stream.DisposeAsync();
			return true;
		}
		catch {
			return false;
		}
	}
}
