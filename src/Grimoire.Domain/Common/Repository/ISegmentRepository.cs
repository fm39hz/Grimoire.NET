namespace Grimoire.Domain.Common.Repository;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Entity.Book;
using Entity.Book.Segment;
using ValueObject;

public interface ISegmentRepository : IRepository<SegmentModel> {
	public Task<IEnumerable<SegmentModel>> FindByChapterPath(BookPath chapterPath, CancellationToken cancellationToken = default);
	public Task DeleteByChapterPath(BookPath chapterPath, CancellationToken cancellationToken = default);
	public Task<IEnumerable<ImageSegmentModel>> FindImageSegmentsBySeriesPath(BookPath seriesPath, CancellationToken cancellationToken = default);
	public Task<IEnumerable<ImageSegmentModel>> FindImageSegmentsByChapterPaths(IEnumerable<BookPath> chapterPaths, CancellationToken cancellationToken = default);
}
