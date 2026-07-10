namespace Grimoire.Domain.Common.Repository;

using System.Threading;
using Entity.Book;
using ValueObject;

public interface IChapterRepository : IRepository<ChapterModel> {
	public Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, CancellationToken cancellationToken = default);
	public Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
	public Task<int> CountByVolumeId(Guid volumeId, CancellationToken cancellationToken = default);
	public Task<IEnumerable<ChapterModel>> FindByVolumeIds(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default);
	public Task<IEnumerable<ChapterModel>> FindByVolumeIdsWithContent(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default);
	public Task<ChapterModel?> FindByVolumeIdAndOrder(Guid volumeId, double order, CancellationToken cancellationToken = default);
	public Task MoveChapterAsync(Guid chapterId, BookPath oldPath, BookPath newPath, double newOrder, CancellationToken cancellationToken = default);
	public Task DeleteSubtreeAsync(Guid chapterId, BookPath path, CancellationToken cancellationToken = default);
}
