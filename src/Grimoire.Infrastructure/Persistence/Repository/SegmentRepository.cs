namespace Grimoire.Infrastructure.Persistence.Repository;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Database;
using Domain.Common.Repository;
using Domain.Common.ValueObject;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Microsoft.EntityFrameworkCore;

public sealed class SegmentRepository(ApplicationDbContext context)
	: CrudRepository<SegmentModel>(context), ISegmentRepository {

	public async Task<IEnumerable<SegmentModel>> FindByChapterPath(BookPath chapterPath, CancellationToken cancellationToken = default) {
		var ltreePath = (LTree)chapterPath.Value;
		return await Entities
			.AsNoTracking()
			.Where(s => s.DbPath.IsDescendantOf(ltreePath))
			.OrderBy(s => s.Order)
			.ToListAsync(cancellationToken);
	}

	public async Task DeleteByChapterPath(BookPath chapterPath, CancellationToken cancellationToken = default) {
		var ltreePath = (LTree)chapterPath.Value;
		await Entities
			.Where(s => s.DbPath.IsDescendantOf(ltreePath))
			.ExecuteDeleteAsync(cancellationToken);
	}

	public async Task<IEnumerable<ImageSegmentModel>> FindImageSegmentsBySeriesPath(BookPath seriesPath, CancellationToken cancellationToken = default) {
		var ltreePath = (LTree)seriesPath.Value;
		return await Entities
			.AsNoTracking()
			.OfType<ImageSegmentModel>()
			.Where(s => s.DbPath.IsDescendantOf(ltreePath))
			.ToListAsync(cancellationToken);
	}

	public async Task<IEnumerable<ImageSegmentModel>> FindImageSegmentsByChapterPaths(IEnumerable<BookPath> chapterPaths, CancellationToken cancellationToken = default) {
		var paths = chapterPaths.Select(p => (LTree)p.Value).ToList();
		if (paths.Count == 0) {
			return [];
		}

		return await Entities
			.AsNoTracking()
			.OfType<ImageSegmentModel>()
			.Where(s => paths.Any(p => s.DbPath.IsDescendantOf(p)))
			.ToListAsync(cancellationToken);
	}

	public async Task<IEnumerable<SegmentModel>> FindByChapterPaths(IEnumerable<BookPath> chapterPaths, CancellationToken cancellationToken = default) {
		var paths = chapterPaths.Select(static path => (LTree)path.Value).ToList();
		if (paths.Count == 0) return [];
		return await Entities.AsNoTracking()
			.Where(segment => paths.Any(path => segment.DbPath.IsDescendantOf(path)))
			.OrderBy(static segment => segment.DbPath)
			.ThenBy(static segment => segment.Order)
			.ToListAsync(cancellationToken);
	}
}
