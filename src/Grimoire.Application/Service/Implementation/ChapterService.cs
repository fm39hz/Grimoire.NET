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
		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			var volumeId = PrefixedId.ToGuid(dto.VolumeId, EntityPrefix.Volume);
			_ = await volumeRepository.FindOne(volumeId, cancellationToken) ??
				throw new EntityNotFoundException($"Volume with id {dto.VolumeId} not found");

			var strategy = strategyFactory.GetStrategy(dto);
			var result = await strategy.ExecuteAsync(dto, volumeId, cancellationToken);

			var existing = await chapterRepository.FindByVolumeIdAndOrder(volumeId, dto.Order, cancellationToken);
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
				await unitOfWork.CommitTransactionAsync(cancellationToken);
				return existing;
			}

			if (result.Source is not null) {
				await sourceRepository.Create(result.Source, cancellationToken);
			}

			result.Chapter.ContentData = result.Content;
			var chapter = await chapterRepository.Create(result.Chapter, cancellationToken);
			await unitOfWork.CommitTransactionAsync(cancellationToken);
			return chapter;
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}

	public async Task<ChapterModel> CreateFromImportAsync(CreateChapterRequestDto dto, CancellationToken cancellationToken = default) => await Create(dto, cancellationToken);

	public async Task<ChapterModel> Update(Guid id, UpdateChapterRequestDto dto, CancellationToken cancellationToken = default) {
		var chapter = await chapterRepository.FindOne(id, cancellationToken) ??
					throw new EntityNotFoundException($"Chapter with id {id} not found");
		mapper.UpdateChapter(dto, chapter);

		if (dto.VolumeId is not null) {
			var newVolumeId = PrefixedId.ToGuid(dto.VolumeId, EntityPrefix.Volume);
			if (chapter.VolumeId != newVolumeId) {
				_ = await volumeRepository.FindOne(newVolumeId, cancellationToken) ??
					throw new EntityNotFoundException($"Volume with id {dto.VolumeId} not found");
				chapter.VolumeId = newVolumeId;
			}
		}

		return await chapterRepository.Update(chapter, cancellationToken);
	}

	public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default) => await chapterRepository.Delete(id, cancellationToken);

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

			foreach (var chapter in chaptersToMerge) {
				await chapterRepository.Delete(chapter.Id, cancellationToken);
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
