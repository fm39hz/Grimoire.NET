namespace Grimoire.Infrastructure.Persistence.Repository;

using System.Threading;
using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class SourceMaterialRepository(ApplicationDbContext context)
	: CrudRepository<SourceMaterial>(context), ISourceMaterialRepository {
	public async Task<IEnumerable<SourceMaterial>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(sm => sm.SeriesId == seriesId)
			.OrderByDescending(sm => sm.CreatedAt)
			.ToListAsync(cancellationToken);
}
