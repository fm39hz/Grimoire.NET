namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common;
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

	public async Task<PagedResult<ChapterModel>> FindByVolumeId(Guid volumeId, int pageIndex, int pageSize) {
		var query = Entities
			.AsNoTracking()
			.AsSplitQuery()
			.Where(c => c.VolumeId == volumeId);
		
		var totalCount = await query.CountAsync();
		var items = await query
			.OrderBy(c => c.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();
		
		return new PagedResult<ChapterModel>(items, totalCount, pageIndex, pageSize);
	}
}
