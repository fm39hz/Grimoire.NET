namespace Grimoire.Application.Service.Implementation;

using Common;
using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using Dto.Common;
using Mapper;
using Strategy;

public sealed class ChapterService(
	IChapterRepository chapterRepository,
	ISourceMaterialRepository sourceRepository,
	IBookMapper mapper,
	IngestionStrategyFactory strategyFactory) : IChapterService {
	public async Task<ChapterModel?> FindOne(Guid id) => await chapterRepository.FindOne(id);

	public async Task<IEnumerable<ChapterModel>> FindAll() => await chapterRepository.FindAll();

	public async Task<PagedResult<ChapterModel>> FindAll(PaginationRequest request) {
		var items = await chapterRepository.FindAll(request.PageIndex, request.PageSize);
		var totalCount = await chapterRepository.CountAll();
		return new PagedResult<ChapterModel>(items.ToList(), totalCount, request.PageIndex, request.PageSize);
	}

	public async Task<ChapterModel> Create(CreateChapterRequestDto dto) {
		// Use strategy pattern to handle different ingestion types
		var strategy = strategyFactory.GetStrategy(dto);
		var result = await strategy.ExecuteAsync(dto);

		// Save SourceMaterial if present (from RawMarkdown ingestion)
		if (result.Source is not null) {
			await sourceRepository.Create(result.Source);
		}

		// Set navigation property to ensure EF Core saves both Chapter and Content
		result.Chapter.ContentData = result.Content;

		// Save chapter with content
		var chapter = await chapterRepository.Create(result.Chapter);

		return chapter;
	}

	public async Task<ChapterModel> CreateFromImportAsync(CreateChapterRequestDto dto) => await Create(dto);

	public async Task<ChapterModel> Update(Guid id, UpdateChapterRequestDto dto) {
		var chapter = await chapterRepository.FindOne(id) ??
					throw new EntityNotFoundException($"Chapter with id {id} not found");
		mapper.UpdateChapter(dto, chapter);
		return await chapterRepository.Update(chapter);
	}

	public async Task<int> Delete(Guid id) => await chapterRepository.Delete(id);
}
