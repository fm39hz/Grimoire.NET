namespace Grimoire.Application.Service.Contract;

using Domain.Entity.Book;

public interface IAssetService {
	Task<AssetModel?> FindOne(Guid id);
	Task<AssetModel?> GetByFileHashAsync(string fileHash);
	Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash);
	Task<AssetModel> Create(AssetModel asset);
	Task<int> Delete(Guid id);
}
