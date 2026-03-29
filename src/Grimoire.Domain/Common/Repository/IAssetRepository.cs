namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface IAssetRepository : IRepository<AssetModel> {
	public Task<AssetModel?> GetByFileHashAsync(string fileHash);
	public Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash);
	public Task<IReadOnlyDictionary<Guid, AssetModel>> FindByIdsAsync(IEnumerable<Guid> assetIds);
}
