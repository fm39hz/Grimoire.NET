namespace Grimoire.Application.Service.Contract;

using Domain.Entity.Book;

public interface IAssetService {
	public Task<AssetModel?> FindOne(Guid id);
	public Task<IReadOnlyDictionary<Guid, AssetModel>> FindByIdsAsync(IEnumerable<Guid> assetIds);
	public Task<AssetModel?> GetByFileHashAsync(string fileHash);
	public Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash);
	public Task<AssetModel> Create(AssetModel asset);
	public Task<int> Delete(Guid id);
}
