namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class VolumeRepository(ApplicationDbContext context)
	: CrudRepository<VolumeModel>(context), IVolumeRepository {
	public async Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId) =>
		await Entities
			.Where(v => v.SeriesId == seriesId)
			.OrderBy(v => v.Order)
			.ToListAsync();
}
