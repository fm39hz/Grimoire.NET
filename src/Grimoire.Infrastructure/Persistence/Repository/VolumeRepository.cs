namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class VolumeRepository(ApplicationDbContext context)
	: CrudRepository<VolumeModel>(context), IVolumeRepository {
	
	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId) =>
		await Entities
			.AsNoTracking()
			.Where(v => v.SeriesId == seriesId)
			.OrderBy(v => v.Order)
			.ToListAsync();

	public async Task<PagedResult<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize) {
		var query = Entities
			.AsNoTracking()
			.Where(v => v.SeriesId == seriesId);
		
		var totalCount = await query.CountAsync();
		var items = await query
			.OrderBy(v => v.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();
		
		return new PagedResult<VolumeModel>(items, totalCount, pageIndex, pageSize);
	}
}
