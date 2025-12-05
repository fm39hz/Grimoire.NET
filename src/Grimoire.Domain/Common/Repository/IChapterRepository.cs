namespace Grimoire.Domain.Common.Repository;

using Entity.Book;

public interface IChapterRepository : IRepository<ChapterModel> {
	public Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId);
}
