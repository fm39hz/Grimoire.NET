namespace Grimoire.Application.Service.Implementation;

using Common;
using Contract;
using DomainCommon = Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using Dto.Common;
using Mapper;
using Strategy;

public sealed class ChapterService(
	IChapterRepository chapterRepository,
	IVolumeRepository volumeRepository,
	ISourceMaterialRepository sourceRepository,
	IBookMapper mapper,
	IngestionStrategyFactory strategyFactory,
	IUnitOfWork unitOfWork) : CrudServiceBase<ChapterModel>, IChapterService {
	public async Task<ChapterModel?> FindOne(Guid id) => await chapterRepository.FindOne(id);

	public async Task<IEnumerable<ChapterModel>> FindAll() => await chapterRepository.FindAll();

	public async Task<PagedResult<ChapterModel>> FindAll(PaginationRequest request) =>
		await GetPagedResultAsync(chapterRepository, request);

	public async Task<ChapterModel> Create(CreateChapterRequestDto dto) {
		// Begin transaction for multi-step operation
		await unitOfWork.BeginTransactionAsync();

		try {
			// Validate that the Volume exists
			var volumeId = DomainCommon.PrefixedId.ToGuid(dto.VolumeId, DomainCommon.EntityPrefix.Volume);
			var volume = await volumeRepository.FindOne(volumeId);
			if (volume == null) {
				throw new EntityNotFoundException($"Volume with id {dto.VolumeId} not found");
			}

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

			// Commit transaction
			await unitOfWork.CommitTransactionAsync();

			return chapter;
		}
		catch {
			// Rollback transaction on any error
			await unitOfWork.RollbackTransactionAsync();
			throw;
		}
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
