namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface IChapterRepository : IRepository<ChapterModel> {
	public Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId);
	public Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, int pageIndex, int pageSize);
	public Task<int> CountByVolumeId(Guid volumeId);
	public Task<IEnumerable<ChapterModel>> FindByVolumeIdsWithContent(IEnumerable<Guid> volumeIds);
}
