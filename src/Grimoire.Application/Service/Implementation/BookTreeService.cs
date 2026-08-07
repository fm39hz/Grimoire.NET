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
using Dto.Book;
using Dto.Book.Tree;
using Dto.Common;
using Grimoire.Domain.Common.ValueObject;
using Mapper;
using Microsoft.EntityFrameworkCore;

public sealed class BookTreeService(
	ISeriesRepository seriesRepository,
	IVolumeRepository volumeRepository,
	IChapterRepository chapterRepository,
	IUnitOfWork unitOfWork,
	IBookMapper mapper) : CrudServiceBase<VolumeModel>, IBookTreeService {

	private const string DefaultShelfId = "bookshelf:default";
	private const string DefaultShelfTitle = "Book Shelf";

	private async Task ExecuteInTransaction(Func<Task> action, CancellationToken cancellationToken) {
		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			await action();
			await unitOfWork.CommitTransactionAsync(cancellationToken);
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}

	private async Task<T> ExecuteInTransaction<T>(Func<Task<T>> action, CancellationToken cancellationToken) {
		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			var result = await action();
			await unitOfWork.CommitTransactionAsync(cancellationToken);
			return result;
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}

	public async Task<BookTreeDto> GetTree(Guid seriesId, bool includeContent = false, CancellationToken cancellationToken = default) {
		var series = await seriesRepository.FindOne(seriesId, cancellationToken)
			?? throw new EntityNotFoundException($"Series with id {seriesId} not found");

		var volumes = (await volumeRepository.FindBySeriesId(seriesId, cancellationToken)).ToList();
		var volumeIds = volumes.Select(v => v.Id).ToList();

		var chapters = (await chapterRepository.FindByVolumeIds(volumeIds, cancellationToken)).ToList();

		var root = new BookTreeNodeDto {
			Id = DefaultShelfId,
			Type = BookTreeNodeType.BookShelf,
			Title = DefaultShelfTitle,
			Children = [
				new BookTreeNodeDto {
					Id = PrefixedId.ToString(EntityPrefix.Series, series.Id),
					Type = BookTreeNodeType.Series,
					Title = series.Title,
					Order = null,
					ParentId = DefaultShelfId,
					Children = [.. volumes.Select(v => new BookTreeNodeDto {
						Id = PrefixedId.ToString(EntityPrefix.Volume, v.Id),
						Type = BookTreeNodeType.Volume,
						Title = v.Title,
						Order = v.Order,
						ParentId = PrefixedId.ToString(EntityPrefix.Series, series.Id),
						Children = [.. chapters.Where(c => c.Path.GetVolumeId() == v.Id).Select(c => new BookTreeNodeDto {
							Id = PrefixedId.ToString(EntityPrefix.Chapter, c.Id),
							Type = BookTreeNodeType.Chapter,
							Title = c.Title,
							Order = c.Order,
							ParentId = PrefixedId.ToString(EntityPrefix.Volume, v.Id),
							Children = []
						})]
					})]
				}
			]
		};

		return new BookTreeDto(root);
	}

	public async Task<SeriesModel?> FindSeries(Guid seriesId, CancellationToken cancellationToken = default) =>
		await seriesRepository.FindOne(seriesId, cancellationToken);

	public async Task<SeriesModel> CreateSeries(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default) => await ExecuteInTransaction(async () => {
		var series = mapper.CreateSeries(dto);
		series.Path = "n" + series.Id.ToString("N");
		return await seriesRepository.Create(series, cancellationToken);
	}, cancellationToken);

	public async Task<(SeriesModel Series, bool Created)> GetOrCreateSeries(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		var normalizedTitle = dto.Title.Trim();
		var existing = await seriesRepository.FindOneByTitle(normalizedTitle, cancellationToken);
		if (existing is not null) {
			return (existing, false);
		}

		var normalizedDto = dto with { Title = normalizedTitle };
		return (await CreateSeries(normalizedDto, cancellationToken), true);
	}

	public async Task<SeriesModel> UpdateSeries(Guid seriesId, UpdateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		var series = await seriesRepository.FindOneTracked(seriesId, cancellationToken) ??
			throw new EntityNotFoundException($"Series with id {seriesId} not found");

		mapper.UpdateSeries(dto, series);

		return await ExecuteInTransaction(async () => await seriesRepository.Update(series, cancellationToken), cancellationToken);
	}

	public async Task<VolumeModel> CreateVolume(CreateVolumeRequestDto dto, CancellationToken cancellationToken = default) {
		var seriesId = PrefixedId.ToGuid(dto.SeriesId, EntityPrefix.Series);
		var series = await seriesRepository.FindOne(seriesId, cancellationToken) ??
			throw new EntityNotFoundException($"Series with id {dto.SeriesId} not found");

		return await ExecuteInTransaction(async () => {
			var volume = mapper.CreateVolume(dto);
			volume.Path = $"{series.Path}.n{volume.Id:N}";
			return await volumeRepository.Create(volume, cancellationToken);
		}, cancellationToken);
	}

	public async Task<VolumeModel> UpdateVolume(Guid volumeId, UpdateVolumeRequestDto dto, CancellationToken cancellationToken = default) {
		var volume = await volumeRepository.FindOneTracked(volumeId, cancellationToken) ??
			throw new EntityNotFoundException($"Volume with id {volumeId} not found");

		mapper.UpdateVolume(dto, volume);

		var result = await ExecuteInTransaction(async () => await volumeRepository.Update(volume, cancellationToken), cancellationToken);

		if (dto.SeriesId is not null) {
			var newParentId = PrefixedId.ToGuid(dto.SeriesId, EntityPrefix.Series);
			await MoveNode(volumeId, newParentId, dto.Order ?? volume.Order, cancellationToken);
			var refreshed = await volumeRepository.FindOneTracked(volumeId, cancellationToken);
			if (refreshed is not null) {
				result = refreshed;
			}
		}

		return result;
	}

	public async Task<IEnumerable<VolumeModel>> FindVolumes(Guid seriesId, CancellationToken cancellationToken = default) =>
		await volumeRepository.FindBySeriesId(seriesId, cancellationToken);

	public async Task<PagedResult<VolumeModel>> FindVolumes(Guid seriesId, PaginationRequest pagination, CancellationToken cancellationToken = default) {
		var items = (await volumeRepository.FindBySeriesId(seriesId, pagination.PageIndex, pagination.PageSize, cancellationToken)).ToList();
		var total = await volumeRepository.CountBySeriesId(seriesId, cancellationToken);
		return new PagedResult<VolumeModel>(items, total, pagination.PageIndex, pagination.PageSize);
	}

	public async Task<IEnumerable<ChapterModel>> FindChapters(Guid volumeId, CancellationToken cancellationToken = default) =>
		await chapterRepository.FindByVolumeId(volumeId, cancellationToken);

	public async Task<PagedResult<ChapterModel>> FindChapters(Guid volumeId, PaginationRequest pagination, CancellationToken cancellationToken = default) {
		var items = (await chapterRepository.FindByVolumeId(volumeId, pagination.PageIndex, pagination.PageSize, cancellationToken)).ToList();
		var total = await chapterRepository.CountByVolumeId(volumeId, cancellationToken);
		return new PagedResult<ChapterModel>(items, total, pagination.PageIndex, pagination.PageSize);
	}

	public async Task MoveNode(Guid nodeId, Guid? newParentId, double newOrder, CancellationToken cancellationToken = default) {
		var volume = await volumeRepository.FindOneTracked(nodeId, cancellationToken);
		if (volume is not null) {
			if (newParentId is null) {
				throw new InvalidOperationException("Volume must have a parent series");
			}

			var series = await seriesRepository.FindOne(newParentId.Value, cancellationToken) ??
				throw new EntityNotFoundException($"Series with id {newParentId} not found");

			var oldPath = volume.Path;
			BookPath newPath = $"{series.Path.Value}.n{volume.Id:N}";

			await ExecuteInTransaction(async () => await volumeRepository.MoveVolumeAsync(volume.Id, oldPath, newPath, newOrder, cancellationToken), cancellationToken);
			return;
		}

		var chapter = await chapterRepository.FindOneTracked(nodeId, cancellationToken);
		if (chapter is not null) {
			if (newParentId is null) {
				throw new InvalidOperationException("Chapter must have a parent volume");
			}

			var parentVolume = await volumeRepository.FindOne(newParentId.Value, cancellationToken) ??
				throw new EntityNotFoundException($"Volume with id {newParentId} not found");

			var oldPath = chapter.Path;
			BookPath newPath = $"{parentVolume.Path.Value}.n{chapter.Id:N}";

			await ExecuteInTransaction(async () => await chapterRepository.MoveChapterAsync(chapter.Id, oldPath, newPath, newOrder, cancellationToken), cancellationToken);
			return;
		}

		throw new EntityNotFoundException($"Node with id {nodeId} not found as Volume or Chapter");
	}

	public async Task<int> DeleteSubtree(Guid nodeId, CancellationToken cancellationToken = default) {
		var series = await seriesRepository.FindOne(nodeId, cancellationToken);
		if (series is not null) {
			await ExecuteInTransaction(async () => await seriesRepository.DeleteSubtreeAsync(series.Id, series.Path, cancellationToken), cancellationToken);
			return 1;
		}

		var volume = await volumeRepository.FindOne(nodeId, cancellationToken);
		if (volume is not null) {
			await ExecuteInTransaction(async () => await volumeRepository.DeleteSubtreeAsync(volume.Id, volume.Path, cancellationToken), cancellationToken);
			return 1;
		}

		var chapter = await chapterRepository.FindOne(nodeId, cancellationToken);
		if (chapter is not null) {
			await ExecuteInTransaction(async () => await chapterRepository.DeleteSubtreeAsync(chapter.Id, chapter.Path, cancellationToken), cancellationToken);
			return 1;
		}

		return 0;
	}

	/// <summary>
	///     Merges multiple volumes into a base volume: every chapter in the target volumes is moved
	///     into the base volume (re-pathing its subtree), then each emptied target volume is deleted.
	///     All volumes must belong to the same series.
	/// </summary>
	public async Task<VolumeModel> MergeVolumesAsync(Guid baseVolumeId, IReadOnlyList<Guid> volumeIds, CancellationToken cancellationToken = default) {
		var baseVolume = await volumeRepository.FindOne(baseVolumeId, cancellationToken) ??
			throw new EntityNotFoundException($"Volume with id {baseVolumeId} not found");
		var baseSeriesId = baseVolume.Path.GetSeriesId();

		var targets = new List<VolumeModel>(volumeIds.Count);
		foreach (var volId in volumeIds) {
			if (volId == baseVolumeId) {
				throw new InvalidOperationException("Base volume cannot also be a merge target");
			}
			var vol = await volumeRepository.FindOne(volId, cancellationToken) ??
				throw new EntityNotFoundException($"Volume with id {volId} not found");
			if (vol.Path.GetSeriesId() != baseSeriesId) {
				throw new InvalidOperationException("All volumes to merge must belong to the same series");
			}
			targets.Add(vol);
		}

		foreach (var vol in targets) {
			var chapters = await chapterRepository.FindByVolumeId(vol.Id, cancellationToken);
			foreach (var chapter in chapters) {
				// Order appended after base's chapters — fractional offset keeps uniqueness.
				await MoveNode(chapter.Id, baseVolumeId, chapter.Order, cancellationToken);
			}
			await DeleteSubtree(vol.Id, cancellationToken);
		}

		return baseVolume;
	}

	/// <summary>
	///     Splits a volume at a chapter boundary: a new volume is created under the same series and
	///     every chapter at/after <c>atChapterOrder</c> is moved into it. Returns the new volume.
	/// </summary>
	public async Task<VolumeModel> SplitVolumeAsync(Guid volumeId, double atChapterOrder, string newVolumeTitle, CancellationToken cancellationToken = default) {
		var volume = await volumeRepository.FindOne(volumeId, cancellationToken) ??
			throw new EntityNotFoundException($"Volume with id {volumeId} not found");
		var seriesId = volume.Path.GetSeriesId();

		var chapters = (await chapterRepository.FindByVolumeId(volumeId, cancellationToken)).ToList();
		var toMove = chapters.Where(c => c.Order >= atChapterOrder).OrderBy(c => c.Order).ToList();
		if (toMove.Count == 0) {
			throw new InvalidOperationException("No chapters at or after the given boundary to move");
		}

		var series = await seriesRepository.FindOne(seriesId, cancellationToken) ??
			throw new EntityNotFoundException($"Series with id {seriesId} not found");

		var newVolume = await CreateVolume(new CreateVolumeRequestDto(
			PrefixedId.ToString(EntityPrefix.Series, seriesId),
			volume.Order + 0.5,
			newVolumeTitle,
			null), cancellationToken);

		// Re-order moved chapters to start at 1 in the new volume.
		var newOrder = 1.0;
		foreach (var chapter in toMove) {
			await MoveNode(chapter.Id, newVolume.Id, newOrder++, cancellationToken);
		}

		return newVolume;
	}

	/// <summary>
	///     Reorders sibling nodes (volumes under a series, or chapters under a volume) by moving each
	///     to its index position. Uses fractional doubles so no sibling renumbering is required.
	/// </summary>
	public async Task ReorderSiblingsAsync(Guid parentId, IReadOnlyList<Guid> orderedChildIds, CancellationToken cancellationToken = default) {
		var parent = await seriesRepository.FindOne(parentId, cancellationToken);
		if (parent is not null) {
			var order = 1.0;
			foreach (var childId in orderedChildIds) {
				await MoveNode(childId, parentId, order++, cancellationToken);
			}
			return;
		}

		var parentVolume = await volumeRepository.FindOne(parentId, cancellationToken);
		if (parentVolume is not null) {
			var order = 1.0;
			foreach (var childId in orderedChildIds) {
				await MoveNode(childId, parentId, order++, cancellationToken);
			}
			return;
		}

		throw new EntityNotFoundException($"Parent with id {parentId} not found as Series or Volume");
	}
}
