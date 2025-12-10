namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
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

	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize) {
		var items = await Entities
			.AsNoTracking()
			.Where(v => v.SeriesId == seriesId)
			.OrderBy(v => v.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

		return items;
	}

	public async Task<int> CountBySeriesId(Guid seriesId) =>
		await Entities
			.AsNoTracking()
			.Where(v => v.SeriesId == seriesId)
			.CountAsync();
}
