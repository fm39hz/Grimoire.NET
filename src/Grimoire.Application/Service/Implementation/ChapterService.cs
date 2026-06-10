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
	INodeManagerService bookTreeService,
	IBookMapper mapper,
	IIngestionStrategyFactory strategyFactory,
	IUnitOfWork unitOfWork) : CrudServiceBase<ChapterModel>, IChapterService {
	public async Task<ChapterModel?> FindOne(Guid id, CancellationToken cancellationToken = default) => await chapterRepository.FindOne(id, cancellationToken);

	public async Task<PagedResult<ChapterModel>> FindAll(PaginationRequest request, CancellationToken cancellationToken = default) =>
		await GetPagedResultAsync(chapterRepository, request, cancellationToken);

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
		var strategy = strategyFactory.GetStrategy(dto);
		var result = await strategy.ExecuteAsync(dto, volumeId, cancellationToken);

		if (existing is not null) {
			existing.Title = result.Chapter.Title;
			existing.Status = result.Chapter.Status;

			if (existing.ContentData != null) {
				existing.ContentData.Segments = result.Content.Segments;
				existing.ContentData.Footnotes = result.Content.Footnotes;
			}
			else {
				existing.ContentData = new ChapterContentModel {
					Id = existing.Id,
					Segments = result.Content.Segments,
					Footnotes = result.Content.Footnotes
				};
			}

			if (result.Source is not null) {
				await sourceRepository.Create(result.Source, cancellationToken);
			}

			await chapterRepository.Update(existing, cancellationToken);
			await bookTreeService.UpdateNode(existing.Id, existing.Title, existing.Order, cancellationToken);
			return (existing, false);
		}

		if (result.Source is not null) {
			await sourceRepository.Create(result.Source, cancellationToken);
		}

		result.Chapter.ContentData = result.Content;
		var chapter = await chapterRepository.Create(result.Chapter, cancellationToken);
		await bookTreeService.CreateNode(chapter.Id, BookNodeType.Chapter, volumeId, chapter.Title, chapter.Order, cancellationToken);
		return (chapter, true);
	}

	public async Task<ChapterModel> Update(Guid id, UpdateChapterRequestDto dto, CancellationToken cancellationToken = default) {
		var chapter = await chapterRepository.FindOne(id, cancellationToken) ??
					throw new EntityNotFoundException($"Chapter with id {id} not found");
		var currentTitle = chapter.Title;
		var currentOrder = chapter.Order;
		mapper.UpdateChapter(dto, chapter);
		if (dto.Title is null) {
			chapter.Title = currentTitle;
		}
		if (dto.Order is null) {
			chapter.Order = currentOrder;
		}

		if (dto.VolumeId is not null) {
			var newVolumeId = PrefixedId.ToGuid(dto.VolumeId, EntityPrefix.Volume);
			if (chapter.VolumeId != newVolumeId) {
				_ = await volumeRepository.FindOne(newVolumeId, cancellationToken) ??
					throw new EntityNotFoundException($"Volume with id {dto.VolumeId} not found");
				chapter.VolumeId = newVolumeId;
				await bookTreeService.MoveNode(chapter.Id, newVolumeId, dto.Order ?? chapter.Order, cancellationToken);
			}
		}

		var updated = await chapterRepository.Update(chapter, cancellationToken);
		await bookTreeService.UpdateNode(updated.Id, updated.Title, updated.Order, cancellationToken);
		return updated;
	}

	public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default) => await bookTreeService.DeleteSubtree(id, cancellationToken);

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

			var firstVolumeId = chapters[0].VolumeId;
			for (var i = 1; i < chapters.Count; i++) {
				if (chapters[i].VolumeId != firstVolumeId) {
					throw new InvalidOperationException("All chapters to merge must belong to the same volume");
				}
			}

			var baseChapter = chapters[0];
			var chaptersToMerge = chapters.Skip(1).ToList();

			baseChapter.Merge(chaptersToMerge);

			await chapterRepository.Update(baseChapter, cancellationToken);
			await bookTreeService.UpdateNode(baseChapter.Id, baseChapter.Title, baseChapter.Order, cancellationToken);

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

			if (originalChapter.ContentData == null) {
				throw new InvalidOperationException("Cannot split a chapter with no content");
			}

			var splitPoints = dto.SplitPoints
				.Select(sp => (sp.SegmentIndex, sp.NewChapterTitle))
				.ToList();

			var splitResult = originalChapter.Split(splitPoints);

			await chapterRepository.Update(splitResult.UpdatedOriginal, cancellationToken);
			await bookTreeService.UpdateNode(
				splitResult.UpdatedOriginal.Id,
				splitResult.UpdatedOriginal.Title,
				splitResult.UpdatedOriginal.Order,
				cancellationToken);

			foreach (var newChapter in splitResult.NewChapters.Skip(1)) {
				await chapterRepository.Create(newChapter, cancellationToken);
				await bookTreeService.CreateNode(
					newChapter.Id,
					BookNodeType.Chapter,
					newChapter.VolumeId,
					newChapter.Title,
					newChapter.Order,
					cancellationToken);
			}

			await unitOfWork.CommitTransactionAsync(cancellationToken);

			return splitResult.NewChapters;
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}
}
