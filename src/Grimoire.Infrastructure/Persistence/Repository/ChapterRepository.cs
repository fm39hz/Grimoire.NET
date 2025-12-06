namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class ChapterRepository(ApplicationDbContext context)
	: CrudRepository<ChapterModel>(context), IChapterRepository {
	
	public async Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId) =>
		await Entities
			.AsNoTracking()
			.AsSplitQuery()
			.Where(c => c.VolumeId == volumeId)
			.OrderBy(c => c.Order)
			.ToListAsync();

	public async Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, int pageIndex, int pageSize) {
		var items = await Entities
			.AsNoTracking()
			.AsSplitQuery()
			.Where(c => c.VolumeId == volumeId)
			.OrderBy(c => c.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();
		
		return items;
	}

	public async Task<int> CountByVolumeId(Guid volumeId) {
		return await Entities
			.AsNoTracking()
			.Where(c => c.VolumeId == volumeId)
			.CountAsync();
	}
}
