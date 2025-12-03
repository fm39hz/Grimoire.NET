namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;

public sealed class ChapterService(IChapterRepository repository) : IChapterService {
	public async Task<ChapterModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<ChapterModel>> FindAll() => await repository.FindAll();

	public async Task<ChapterModel> Create(ChapterModel entity) => await repository.Create(entity);

	public async Task<ChapterModel> Update(Guid id, ChapterModel entity) {
		var chapter = new ChapterModel {
			Id = id,
			VolumeId = entity.VolumeId,
			Order = entity.Order,
			Title = entity.Title,
			Content = entity.Content
		};

		return await repository.Update(chapter);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);
}
