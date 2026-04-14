namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Domain.Service;
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

	public async Task<PagedResult<ChapterModel>> FindAll(PaginationRequest request) =>
		await GetPagedResultAsync(chapterRepository, request);

	public async Task<ChapterModel> Create(CreateChapterRequestDto dto) {
		// Begin transaction for multistep operation
		await unitOfWork.BeginTransactionAsync();

		try {
			// Validate that the Volume exists
			var volumeId = PrefixedId.ToGuid(dto.VolumeId, EntityPrefix.Volume);
			var volume = await volumeRepository.FindOne(volumeId) ??
						throw new EntityNotFoundException($"Volume with id {dto.VolumeId} not found");

			// Use strategy pattern to handle different ingestion types
			var strategy = strategyFactory.GetStrategy(dto);
			var result = await strategy.ExecuteAsync(dto, volumeId);

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

	public async Task<IEnumerable<ChapterModel>> SplitAsync(Guid chapterId, SplitChapterRequestDto dto) {
		await unitOfWork.BeginTransactionAsync();

		try {
			var originalChapter = await chapterRepository.FindOne(chapterId) ??
								throw new EntityNotFoundException($"Chapter with id {chapterId} not found");

			if (originalChapter.ContentData == null) {
				throw new InvalidOperationException("Cannot split a chapter with no content");
			}

			var splitPoints = dto.SplitPoints
				.Select(sp => (sp.SegmentIndex, sp.NewChapterTitle))
				.ToList();

			var splitResult = originalChapter.Split(splitPoints);

			await chapterRepository.Update(splitResult.UpdatedOriginal);

			foreach (var newChapter in splitResult.NewChapters) {
				await chapterRepository.Create(newChapter);
			}

			await unitOfWork.CommitTransactionAsync();

			return splitResult.NewChapters.Prepend(splitResult.UpdatedOriginal);
		}
		catch {
			await unitOfWork.RollbackTransactionAsync();
			throw;
		}
	}
}
