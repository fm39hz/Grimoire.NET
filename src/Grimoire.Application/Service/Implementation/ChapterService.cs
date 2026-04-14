namespace Grimoire.Application.Service.Implementation;

using Common;
using Contract;
using Domain.Common;
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
		// Begin transaction for multistep operation
		await unitOfWork.BeginTransactionAsync();

		try {
			// Find the original chapter with content
			var originalChapter = await chapterRepository.FindOne(chapterId) ??
								throw new EntityNotFoundException($"Chapter with id {chapterId} not found");

			if (originalChapter.ContentData == null || originalChapter.ContentData.Segments.Count == 0) {
				throw new InvalidOperationException("Cannot split a chapter with no content");
			}

			var segments = originalChapter.ContentData.Segments;
			var footnotes = originalChapter.ContentData.Footnotes;

			// Validate all split points are within bounds
			foreach (var splitPoint in dto.SplitPoints.Where(splitPoint => splitPoint.SegmentIndex >= segments.Count)) {
				throw new InvalidOperationException(
					$"SegmentIndex {splitPoint.SegmentIndex} is out of bounds (max: {segments.Count - 1})");
			}

			var resultChapters = new List<ChapterModel>();
			var currentIndex = 0;
			var orderIncrement = 0.1f;

			// First chapter: update the original chapter with segments before first split
			var firstSplitIndex = dto.SplitPoints[0].SegmentIndex;
			originalChapter.ContentData.Segments = segments.Take(firstSplitIndex).ToList();

			// Extract footnotes referenced by segments in the original chapter
			var firstChapterFootnoteIds = FootnoteRemapper.ExtractReferencedIds(originalChapter.ContentData.Segments);
			originalChapter.ContentData.Footnotes = footnotes
				.Where(f => firstChapterFootnoteIds.Contains(f.Id.ToString()))
				.ToList();

			await chapterRepository.Update(originalChapter);
			resultChapters.Add(originalChapter);
			currentIndex = firstSplitIndex;

			// Create new chapters for each split point
			for (var i = 0; i < dto.SplitPoints.Count; i++) {
				var splitPoint = dto.SplitPoints[i];
				var nextIndex = i < dto.SplitPoints.Count - 1
					? dto.SplitPoints[i + 1].SegmentIndex
					: segments.Count;

				var newChapterSegments = segments.Skip(currentIndex).Take(nextIndex - currentIndex).ToList();
				var newChapterFootnoteIds = FootnoteRemapper.ExtractReferencedIds(newChapterSegments);
				var newChapterFootnotes = footnotes
					.Where(f => newChapterFootnoteIds.Contains(f.Id.ToString()))
					.ToList();

				var newChapter = new ChapterModel {
					Id = Guid.CreateVersion7(),
					VolumeId = originalChapter.VolumeId,
					Title = splitPoint.NewChapterTitle,
					Order = originalChapter.Order + (orderIncrement * (i + 1)),
					Status = originalChapter.Status,
					ContentData = new ChapterContentModel {
						Id = Guid.CreateVersion7(),
						Segments = newChapterSegments,
						Footnotes = newChapterFootnotes
					}
				};

				var createdChapter = await chapterRepository.Create(newChapter);
				resultChapters.Add(createdChapter);
				currentIndex = nextIndex;
			}

			await unitOfWork.CommitTransactionAsync();

			return resultChapters;
		}
		catch {
			await unitOfWork.RollbackTransactionAsync();
			throw;
		}
	}
}
