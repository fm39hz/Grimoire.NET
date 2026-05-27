namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;

public sealed class AssetService(IAssetRepository repository) : IAssetService {
	public async Task<AssetModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IReadOnlyDictionary<Guid, AssetModel>> FindByIdsAsync(IEnumerable<Guid> assetIds) =>
		await repository.FindByIdsAsync(assetIds);

	public async Task<AssetModel?> GetByFileHashAsync(string fileHash) =>
		await repository.GetByFileHashAsync(fileHash);

	public async Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash) =>
		await repository.GetBySeriesAndFileHashAsync(seriesId, fileHash);

	public async Task<AssetModel> Create(AssetModel asset) => await repository.Create(asset);

	public async Task<int> Delete(Guid id) => await repository.Delete(id);
}
