namespace Grimoire.Application.Service.Contract;

using System.Threading;
using Domain.Entity.Book;

public interface IAssetService {
	public Task<AssetModel?> FindOne(Guid id, CancellationToken cancellationToken = default);
	public Task<IReadOnlyDictionary<Guid, AssetModel>> FindByIdsAsync(IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default);
	public Task<AssetModel?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default);
	public Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash, CancellationToken cancellationToken = default);
	public Task<AssetModel> Create(AssetModel asset, CancellationToken cancellationToken = default);
	public Task<int> Delete(Guid id, CancellationToken cancellationToken = default);
}
