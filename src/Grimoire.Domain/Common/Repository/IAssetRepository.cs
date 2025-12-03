namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface IAssetRepository : IRepository<AssetModel> {
	public Task<AssetModel?> GetByFileHashAsync(string fileHash);
}
