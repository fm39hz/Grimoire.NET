namespace Grimoire.Application.Export;

using System.Threading;
using Domain.Common;
using Domain.Entity.Book;
using Service.Contract;

public class CoverResolver(IAssetService assetService, IStorageService storageService) {
	public async Task<(AssetModel? asset, Func<Task<Stream?>>? streamProvider)> ResolveAsync(
		SeriesModel series,
		CancellationToken cancellationToken = default) {
		var coverKey = series.Metadata?.CoverImage;
		if (string.IsNullOrEmpty(coverKey)) {
			return (null, null);
		}

		if (!PrefixedId.TryToGuid(coverKey, EntityPrefix.Asset, out var id)) {
			return (null, null);
		}

		var asset = await assetService.FindOne(id, cancellationToken);
		if (asset == null) {
			return (null, null);
		}

		// TODO: Replace stream probe with IStorageRepository.ExistsAsync(id)
		// once the storage layer interface supports it.
		if (!await StreamExistsAsync(id, cancellationToken)) {
			return (null, null);
		}

		return (asset, async () => (await storageService.GetFileStreamAsync(id, cancellationToken))?.Stream);
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
