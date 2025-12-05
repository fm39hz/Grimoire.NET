namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using Dto.Common;
using Extensions;
using Mapper;

public sealed class ChapterService(IChapterRepository repository, IBookMapper mapper) : IChapterService {
	public async Task<ChapterModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<ChapterModel>> FindAll() => await repository.FindAll();

	public async Task<PagedResult<ChapterModel>> FindAll(PaginationRequest request) {
		var allItems = await repository.FindAll();
		return allItems.ToPagedList(request);
	}

	public async Task<ChapterModel> Create(CreateChapterRequestDto dto) {
		var chapter = mapper.CreateChapter(dto);
		return await repository.Create(chapter);
	}

	public async Task<ChapterModel> CreateFromImportAsync(CreateChapterRequestDto dto) => await Create(dto);

	public async Task<ChapterModel> Update(Guid id, UpdateChapterRequestDto dto) {
		var chapter = await repository.FindOne(id) ??
					throw new EntityNotFoundException($"Chapter with id {id} not found");
		mapper.UpdateChapter(dto, chapter);
		return await repository.Update(chapter);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);
}
