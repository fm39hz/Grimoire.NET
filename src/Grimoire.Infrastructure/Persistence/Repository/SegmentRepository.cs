namespace Grimoire.Infrastructure.Persistence.Repository;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using Microsoft.EntityFrameworkCore;

public sealed class SegmentRepository(ApplicationDbContext context)
	: CrudRepository<SegmentModel>(context), ISegmentRepository {

	public async Task<IEnumerable<SegmentModel>> FindByChapterPath(LTree chapterPath, CancellationToken cancellationToken = default) {
		return await Entities
			.AsNoTracking()
			.Where(s => s.Path.IsDescendantOf(chapterPath))
			.OrderBy(s => s.Order)
			.ToListAsync(cancellationToken);
	}

	public async Task DeleteByChapterPath(LTree chapterPath, CancellationToken cancellationToken = default) {
		await Entities
			.Where(s => s.Path.IsDescendantOf(chapterPath))
			.ExecuteDeleteAsync(cancellationToken);
	}

	public async Task<IEnumerable<ImageSegmentModel>> FindImageSegmentsBySeriesPath(LTree seriesPath, CancellationToken cancellationToken = default) {
		return await Entities
			.AsNoTracking()
			.OfType<ImageSegmentModel>()
			.Where(s => s.Path.IsDescendantOf(seriesPath))
			.ToListAsync(cancellationToken);
	}

	public async Task<IEnumerable<ImageSegmentModel>> FindImageSegmentsByChapterPaths(IEnumerable<LTree> chapterPaths, CancellationToken cancellationToken = default) {
		var paths = chapterPaths.ToList();
		if (paths.Count == 0) return [];
		return await Entities
			.AsNoTracking()
			.OfType<ImageSegmentModel>()
			.Where(s => paths.Any(p => s.Path.IsDescendantOf(p)))
			.ToListAsync(cancellationToken);
	}
}
