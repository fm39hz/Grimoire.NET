namespace Grimoire.Domain.Common.Repository;

using System.Threading;
using Entity.Book;

public interface IAssetRepository : IRepository<AssetModel> {
	public Task<AssetModel?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default);
	public Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash, CancellationToken cancellationToken = default);
	public Task<IReadOnlyDictionary<Guid, AssetModel>> FindByIdsAsync(IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default);
}
