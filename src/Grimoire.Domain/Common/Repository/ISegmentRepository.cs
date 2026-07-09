namespace Grimoire.Domain.Common.Repository;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Entity.Book;
using Entity.Book.Segment;

using Microsoft.EntityFrameworkCore;

public interface ISegmentRepository : IRepository<SegmentModel> {
	public Task<IEnumerable<SegmentModel>> FindByChapterPath(LTree chapterPath, CancellationToken cancellationToken = default);
	public Task DeleteByChapterPath(LTree chapterPath, CancellationToken cancellationToken = default);
	public Task<IEnumerable<ImageSegmentModel>> FindImageSegmentsBySeriesPath(LTree seriesPath, CancellationToken cancellationToken = default);
	public Task<IEnumerable<ImageSegmentModel>> FindImageSegmentsByChapterPaths(IEnumerable<LTree> chapterPaths, CancellationToken cancellationToken = default);
}
