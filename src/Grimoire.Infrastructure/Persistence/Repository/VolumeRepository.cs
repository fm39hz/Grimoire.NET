namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using System.Threading;
using Microsoft.EntityFrameworkCore;

public sealed class VolumeRepository(ApplicationDbContext context)
	: CrudRepository<VolumeModel>(context), IVolumeRepository {
	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(v => v.SeriesId == seriesId)
			.OrderBy(v => v.Order)
			.ToListAsync(cancellationToken);

	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
		var items = await Entities
			.AsNoTracking()
			.Where(v => v.SeriesId == seriesId)
			.OrderBy(v => v.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

		return items;
	}

	public async Task<int> CountBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(v => v.SeriesId == seriesId)
			.CountAsync(cancellationToken);
}
