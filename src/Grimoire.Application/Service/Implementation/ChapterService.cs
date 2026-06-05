namespace Grimoire.Application.Service.Implementation;

using System.Threading;
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
	public async Task<ChapterModel?> FindOne(Guid id, CancellationToken cancellationToken = default) => await chapterRepository.FindOne(id, cancellationToken);

	public async Task<PagedResult<ChapterModel>> FindAll(PaginationRequest request, CancellationToken cancellationToken = default) =>
		await GetPagedResultAsync(chapterRepository, request, cancellationToken);

	public async Task<ChapterModel> Create(CreateChapterRequestDto dto, CancellationToken cancellationToken = default) {
		// Begin transaction for multistep operation
		await unitOfWork.BeginTransactionAsync(cancellationToken);

		try {
			// Validate that the Volume exists
			var volumeId = PrefixedId.ToGuid(dto.VolumeId, EntityPrefix.Volume);
			var volume = await volumeRepository.FindOne(volumeId, cancellationToken) ??
						throw new EntityNotFoundException($"Volume with id {dto.VolumeId} not found");

			// Use strategy pattern to handle different ingestion types
			var strategy = strategyFactory.GetStrategy(dto);
			var result = await strategy.ExecuteAsync(dto, volumeId, cancellationToken);

			// Save SourceMaterial if present (from RawMarkdown ingestion)
			if (result.Source is not null) {
				await sourceRepository.Create(result.Source, cancellationToken);
			}

			// Set navigation property to ensure EF Core saves both Chapter and Content
			result.Chapter.ContentData = result.Content;

			// Save chapter with content
			var chapter = await chapterRepository.Create(result.Chapter, cancellationToken);

			// Commit transaction
			await unitOfWork.CommitTransactionAsync(cancellationToken);

			return chapter;
		}
		catch {
			// Rollback transaction on any error
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}

	public async Task<ChapterModel> CreateFromImportAsync(CreateChapterRequestDto dto, CancellationToken cancellationToken = default) => await Create(dto, cancellationToken);

	public async Task<ChapterModel> Update(Guid id, UpdateChapterRequestDto dto, CancellationToken cancellationToken = default) {
		var chapter = await chapterRepository.FindOne(id, cancellationToken) ??
					throw new EntityNotFoundException($"Chapter with id {id} not found");
		mapper.UpdateChapter(dto, chapter);
		return await chapterRepository.Update(chapter, cancellationToken);
	}

	public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default) => await chapterRepository.Delete(id, cancellationToken);

	public async Task<IEnumerable<ChapterModel>> SplitAsync(Guid chapterId, SplitChapterRequestDto dto, CancellationToken cancellationToken = default) {
		await unitOfWork.BeginTransactionAsync(cancellationToken);

		try {
			var originalChapter = await chapterRepository.FindOne(chapterId, cancellationToken) ??
								throw new EntityNotFoundException($"Chapter with id {chapterId} not found");

			if (originalChapter.ContentData == null) {
				throw new InvalidOperationException("Cannot split a chapter with no content");
			}

			var splitPoints = dto.SplitPoints
				.Select(sp => (sp.SegmentIndex, sp.NewChapterTitle))
				.ToList();

			var splitResult = originalChapter.Split(splitPoints);

			await chapterRepository.Update(splitResult.UpdatedOriginal, cancellationToken);

			foreach (var newChapter in splitResult.NewChapters) {
				await chapterRepository.Create(newChapter, cancellationToken);
			}

			await unitOfWork.CommitTransactionAsync(cancellationToken);

			return splitResult.NewChapters.Prepend(splitResult.UpdatedOriginal);
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}
}
