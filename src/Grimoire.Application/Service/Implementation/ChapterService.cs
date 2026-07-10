namespace Grimoire.Application.Service.Implementation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Domain.Service;
using Dto.Book;
using Dto.Common;
using Mapper;
using Pipeline.Ingestion;
using Strategy;

public sealed class ChapterService(
	IChapterRepository chapterRepository,
	IVolumeRepository volumeRepository,
	ISourceMaterialRepository sourceRepository,
	ISegmentRepository segmentRepository,
	INodeManagerService bookTreeService,
	IBookMapper mapper,
	IIngestionStrategyFactory strategyFactory,
	IUnitOfWork unitOfWork,
	IngestionCoordinator ingestionCoordinator) : CrudServiceBase<ChapterModel>, IChapterService {

	public async Task<ChapterModel?> FindOne(Guid id, CancellationToken cancellationToken = default) =>
		await chapterRepository.FindOne(id, cancellationToken);

	public async Task<PagedResult<ChapterModel>> FindAll(PaginationRequest request, CancellationToken cancellationToken = default) =>
		await GetPagedResultAsync(chapterRepository, request, cancellationToken);

	public async Task<(ChapterModel Chapter, IEnumerable<SegmentModel> Segments)?> GetWithContentAsync(Guid id, CancellationToken cancellationToken = default) {
		var chapter = await chapterRepository.FindOne(id, cancellationToken);
		if (chapter is null) {
			return null;
		}

		var segments = await segmentRepository.FindByChapterPath(chapter.Path, cancellationToken);
		return (chapter, segments);
	}

	public async Task<ChapterModel> Create(CreateChapterRequestDto dto, CancellationToken cancellationToken = default) {
		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			var volumeId = PrefixedId.ToGuid(dto.VolumeId, EntityPrefix.Volume);
			_ = await volumeRepository.FindOne(volumeId, cancellationToken) ??
				throw new EntityNotFoundException($"Volume with id {dto.VolumeId} not found");

			var (chapter, _) = await UpsertAsync(volumeId, dto, cancellationToken);
			await unitOfWork.CommitTransactionAsync(cancellationToken);
			return chapter;
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}

	public async Task<(ChapterModel Chapter, bool Created)> UpsertAsync(Guid volumeId, CreateChapterRequestDto dto, CancellationToken cancellationToken = default) {
		var existing = await chapterRepository.FindByVolumeIdAndOrder(volumeId, dto.Order, cancellationToken);
		return await UpsertAsync(volumeId, dto, existing, cancellationToken);
	}

	public async Task<(ChapterModel Chapter, bool Created)> UpsertAsync(Guid volumeId, CreateChapterRequestDto dto, ChapterModel? existing, CancellationToken cancellationToken = default) {
		var context = new IngestionContext(volumeId, dto) {
			ExistingChapter = existing
		};

		await ingestionCoordinator.ExecuteAsync(context, cancellationToken);

		return (context.Chapter, existing == null);
	}

	public async Task<ChapterModel> Update(Guid id, UpdateChapterRequestDto dto, CancellationToken cancellationToken = default) {
		var chapter = await chapterRepository.FindOneTracked(id, cancellationToken) ??
					throw new EntityNotFoundException($"Chapter with id {id} not found");

		mapper.UpdateChapter(dto, chapter);

		if (dto.VolumeId is not null) {
			var newVolumeId = PrefixedId.ToGuid(dto.VolumeId, EntityPrefix.Volume);
			var parentGuid = chapter.Path.GetVolumeId();
			if (parentGuid != Guid.Empty && parentGuid != newVolumeId) {
				_ = await volumeRepository.FindOne(newVolumeId, cancellationToken) ??
					throw new EntityNotFoundException($"Volume with id {dto.VolumeId} not found");
				await bookTreeService.MoveNode(chapter.Id, newVolumeId, dto.Order ?? chapter.Order, cancellationToken);
			}
		}

		var updated = await chapterRepository.Update(chapter, cancellationToken);
		return updated;
	}

	public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default) =>
		await bookTreeService.DeleteSubtree(id, cancellationToken);

	public async Task<ChapterModel> MergeAsync(MergeChaptersRequestDto dto, CancellationToken cancellationToken = default) {
		await unitOfWork.BeginTransactionAsync(cancellationToken);

		try {
			if (dto.ChapterIds.Count < 2) {
				throw new InvalidOperationException("At least two chapters are required to merge");
			}

			var chapterIds = dto.ChapterIds
				.Select(id => PrefixedId.ToGuid(id, EntityPrefix.Chapter))
				.ToList();

			var chapters = new List<ChapterModel>(chapterIds.Count);
			foreach (var id in chapterIds) {
				var chapter = await chapterRepository.FindOne(id, cancellationToken)
					?? throw new EntityNotFoundException($"Chapter with id {id} not found");
				chapters.Add(chapter);
			}

			var firstVolumeId = chapters[0].Path.GetVolumeId();
			for (var i = 1; i < chapters.Count; i++) {
				var volId = chapters[i].Path.GetVolumeId();
				if (volId != firstVolumeId) {
					throw new InvalidOperationException("All chapters to merge must belong to the same volume");
				}
			}

			var baseChapter = chapters[0];
			var chaptersToMerge = chapters.Skip(1).ToList();

			// Load segments for base chapter and merge targets
			var baseSegments = (await segmentRepository.FindByChapterPath(baseChapter.Path, cancellationToken)).ToList();
			var mergeList = new List<(ChapterModel Chapter, IReadOnlyList<SegmentModel> Segments)>();
			foreach (var chapter in chaptersToMerge) {
				var segments = (await segmentRepository.FindByChapterPath(chapter.Path, cancellationToken)).ToList();
				mergeList.Add((chapter, segments));
			}

			var mergeResult = ChapterMerger.Merge(baseChapter, baseSegments, mergeList);

			// Delete segments of base chapter and target chapters in DB to overwrite with new merged list
			await segmentRepository.DeleteByChapterPath(baseChapter.Path, cancellationToken);
			foreach (var chapter in chaptersToMerge) {
				await segmentRepository.DeleteByChapterPath(chapter.Path, cancellationToken);
			}

			// Save the updated segments
			await segmentRepository.CreateBulk(mergeResult.UpdatedSegments, cancellationToken);

			await chapterRepository.Update(baseChapter, cancellationToken);

			// Delete merged chapters
			foreach (var chapter in chaptersToMerge) {
				await bookTreeService.DeleteSubtree(chapter.Id, cancellationToken);
			}

			await unitOfWork.CommitTransactionAsync(cancellationToken);

			return baseChapter;
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}

	public async Task<IEnumerable<ChapterModel>> SplitAsync(Guid chapterId, SplitChapterRequestDto dto, CancellationToken cancellationToken = default) {
		await unitOfWork.BeginTransactionAsync(cancellationToken);

		try {
			var originalChapter = await chapterRepository.FindOne(chapterId, cancellationToken) ??
								throw new EntityNotFoundException($"Chapter with id {chapterId} not found");

			var allSegments = (await segmentRepository.FindByChapterPath(originalChapter.Path, cancellationToken)).ToList();
			if (allSegments.Count == 0) {
				throw new InvalidOperationException("Cannot split a chapter with no content");
			}

			var splitPoints = dto.SplitPoints
				.Select(sp => (sp.SegmentIndex, sp.NewChapterTitle))
				.ToList();

			var splitResult = ChapterSplitter.Split(originalChapter, allSegments, splitPoints);

			// Overwrite original chapter segments
			await segmentRepository.DeleteByChapterPath(originalChapter.Path, cancellationToken);

			// Save new chapters
			foreach (var newChapter in splitResult.NewChapters.Skip(1)) {
				await chapterRepository.Create(newChapter, cancellationToken);
			}

			// Save all updated segments (both for original chapter and new chapters)
			await segmentRepository.CreateBulk(splitResult.UpdatedSegments, cancellationToken);

			await chapterRepository.Update(splitResult.UpdatedOriginal, cancellationToken);

			await unitOfWork.CommitTransactionAsync(cancellationToken);

			return splitResult.NewChapters;
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}

	public async Task<(IEnumerable<ChapterModel> Chapters, int CreatedCount, int UpdatedCount)> UpsertBulkAsync(
		Guid seriesId,
		List<(Guid VolumeId, CreateChapterRequestDto Dto)> chapters,
		Action<int>? onProgress = null,
		CancellationToken cancellationToken = default) {

		var volumes = await volumeRepository.FindBySeriesId(seriesId, cancellationToken);
		var volumeIds = Enumerable.Select(volumes, v => v.Id).ToList();

		var existingChapters = (await chapterRepository.FindByVolumeIds(volumeIds, cancellationToken))
			.ToDictionary(c => (c.Path.GetVolumeId(), c.Order));

		var results = new List<ChapterModel>();
		var createdCount = 0;
		var updatedCount = 0;
		var processed = 0;

		foreach (var (volId, dto) in chapters) {
			existingChapters.TryGetValue((volId, dto.Order), out var existing);

			var context = new IngestionContext(volId, dto) {
				ExistingChapter = existing
			};

			await ingestionCoordinator.ExecuteAsync(context, cancellationToken);

			results.Add(context.Chapter);
			if (existing == null) {
				createdCount++;
			}
			else {
				updatedCount++;
			}

			processed++;
			onProgress?.Invoke((int)((double)processed / chapters.Count * 100));
		}

		return (results, createdCount, updatedCount);
	}
}
