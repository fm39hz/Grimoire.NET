namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class SourceMaterialRepository(ApplicationDbContext context)
	: CrudRepository<SourceMaterial>(context), ISourceMaterialRepository {
	
	public async Task<IEnumerable<SourceMaterial>> FindBySeriesId(Guid seriesId) =>
		await Entities
			.AsNoTracking()
			.Where(sm => sm.SeriesId == seriesId)
			.OrderByDescending(sm => sm.CreatedAt)
			.ToListAsync();
}
