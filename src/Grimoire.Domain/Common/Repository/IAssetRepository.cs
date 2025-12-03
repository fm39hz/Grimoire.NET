namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface IAssetRepository : IRepository<AssetModel> {
	public Task<AssetModel?> GetByFileHashAsync(string fileHash);
	public Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash);
}
