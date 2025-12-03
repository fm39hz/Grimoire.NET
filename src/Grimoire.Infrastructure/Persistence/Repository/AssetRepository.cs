namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public class AssetRepository(ApplicationDbContext dbContext)
	: CrudRepository<AssetModel>(dbContext), IAssetRepository {
	public async Task<AssetModel?> GetByFileHashAsync(string fileHash) =>
		await Entities
			.FirstOrDefaultAsync(a => a.FileHash == fileHash);
}
