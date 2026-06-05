namespace Grimoire.Application.Service.Implementation;

using System.Threading;
using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;

public sealed class AssetService(IAssetRepository repository) : IAssetService {
	public async Task<AssetModel?> FindOne(Guid id, CancellationToken cancellationToken = default) => await repository.FindOne(id, cancellationToken);

	public async Task<IReadOnlyDictionary<Guid, AssetModel>> FindByIdsAsync(IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default) =>
		await repository.FindByIdsAsync(assetIds, cancellationToken);

	public async Task<AssetModel?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default) =>
		await repository.GetByFileHashAsync(fileHash, cancellationToken);

	public async Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash, CancellationToken cancellationToken = default) =>
		await repository.GetBySeriesAndFileHashAsync(seriesId, fileHash, cancellationToken);

	public async Task<AssetModel> Create(AssetModel asset, CancellationToken cancellationToken = default) => await repository.Create(asset, cancellationToken);

	public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default) => await repository.Delete(id, cancellationToken);
}
