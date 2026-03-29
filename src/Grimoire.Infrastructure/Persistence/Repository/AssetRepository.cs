namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public class AssetRepository(ApplicationDbContext dbContext)
	: CrudRepository<AssetModel>(dbContext), IAssetRepository {
	public async Task<AssetModel?> GetByFileHashAsync(string fileHash) =>
		await Entities
			.AsNoTracking()
			.FirstOrDefaultAsync(a => a.FileHash == fileHash);

	public async Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash) =>
		await Entities
			.AsNoTracking()
			.FirstOrDefaultAsync(a => a.SeriesId == seriesId && a.FileHash == fileHash);

	public async Task<IReadOnlyDictionary<Guid, AssetModel>> FindByIdsAsync(IEnumerable<Guid> assetIds) =>
		await Entities
			.AsNoTracking()
			.Where(a => assetIds.Contains(a.Id))
			.ToDictionaryAsync(a => a.Id, a => a);
}
