namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using System.Threading;
using Microsoft.EntityFrameworkCore;

public class AssetRepository(ApplicationDbContext dbContext)
	: CrudRepository<AssetModel>(dbContext), IAssetRepository {
	public async Task<AssetModel?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.FirstOrDefaultAsync(a => a.FileHash == fileHash, cancellationToken);

	public async Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.FirstOrDefaultAsync(a => a.SeriesId == seriesId && a.FileHash == fileHash, cancellationToken);

	public async Task<IReadOnlyDictionary<Guid, AssetModel>> FindByIdsAsync(IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(a => assetIds.Contains(a.Id))
			.ToDictionaryAsync(a => a.Id, a => a, cancellationToken);
}
