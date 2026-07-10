namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Common.ValueObject;
using Domain.Entity.Book;
using System.Threading;
using Microsoft.EntityFrameworkCore;

public sealed class SeriesRepository(ApplicationDbContext context)
	: CrudRepository<SeriesModel>(context), ISeriesRepository {
	public async Task<SeriesModel?> FindOneByTitle(string title, CancellationToken cancellationToken = default) =>
		await Context.Series.AsNoTracking().FirstOrDefaultAsync(s => s.Title == title, cancellationToken);

	public async Task DeleteSubtreeAsync(Guid seriesId, BookPath path, CancellationToken cancellationToken = default) {
		LTree ltreePath = (LTree)path.Value;
		await context.Segments.Where(s => s.DbPath.IsDescendantOf(ltreePath)).ExecuteDeleteAsync(cancellationToken);
		await context.Chapters.Where(c => c.DbPath.IsDescendantOf(ltreePath)).ExecuteDeleteAsync(cancellationToken);
		await context.Volumes.Where(v => v.DbPath.IsDescendantOf(ltreePath)).ExecuteDeleteAsync(cancellationToken);
		await Entities.Where(s => s.Id == seriesId).ExecuteDeleteAsync(cancellationToken);
	}
}
