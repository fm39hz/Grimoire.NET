namespace Grimoire.Tests.TestInfrastructure;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Domain.Common;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Common.ValueObject;
using Grimoire.Domain.Entity;
using Grimoire.Domain.Entity.Book;
using Grimoire.Domain.Entity.Book.Segment;

public abstract class InMemoryRepository<T> : IRepository<T> where T : BaseModel {
	public List<T> Items { get; } = [];

	public virtual Task<T?> FindOne(Guid id, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

	public virtual Task<T?> FindOneTracked(Guid id, CancellationToken cancellationToken = default) =>
		FindOne(id, cancellationToken);

	public Task<PagedResult<T>> FindAll(int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
		var items = Items.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
		return Task.FromResult(new PagedResult<T>(items, Items.Count, pageIndex, pageSize));
	}

	public virtual Task<T> Create(T entity, CancellationToken cancellationToken = default) {
		Items.Add(entity);
		return Task.FromResult(entity);
	}

	public Task<IEnumerable<T>> CreateBulk(IEnumerable<T> entities, CancellationToken cancellationToken = default) {
		var list = entities.ToList();
		Items.AddRange(list);
		return Task.FromResult<IEnumerable<T>>(list);
	}

	public virtual Task<T> Update(T entity, CancellationToken cancellationToken = default) {
		var index = Items.FindIndex(i => i.Id == entity.Id);
		if (index >= 0) {
			Items[index] = entity;
		}

		return Task.FromResult(entity);
	}

	public Task<IEnumerable<T>> UpdateBulk(IEnumerable<T> entities, CancellationToken cancellationToken = default) {
		var list = entities.ToList();
		foreach (var entity in list) {
			var index = Items.FindIndex(i => i.Id == entity.Id);
			if (index >= 0) {
				Items[index] = entity;
			}
		}
		return Task.FromResult<IEnumerable<T>>(list);
	}

	public Task<IEnumerable<T>> FindByIds(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) {
		var set = ids.ToHashSet();
		return Task.FromResult<IEnumerable<T>>([.. Items.Where(i => set.Contains(i.Id))]);
	}

	public virtual Task<int> Delete(Guid id, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.RemoveAll(i => i.Id == id));

	public virtual Task<int> DeleteMany(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) {
		var set = ids.ToHashSet();
		return Task.FromResult(Items.RemoveAll(i => set.Contains(i.Id)));
	}
}

public sealed class InMemorySegmentRepository : InMemoryRepository<SegmentModel>, ISegmentRepository {
	public Task<IEnumerable<SegmentModel>> FindByChapterPath(BookPath chapterPath, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<SegmentModel>>([.. Items.Where(s => s.Path.IsDescendantOf(chapterPath)).OrderBy(s => s.Order)]);

	public Task DeleteByChapterPath(BookPath chapterPath, CancellationToken cancellationToken = default) {
		Items.RemoveAll(s => s.Path.IsDescendantOf(chapterPath));
		return Task.CompletedTask;
	}

	public Task<IEnumerable<ImageSegmentModel>> FindImageSegmentsBySeriesPath(BookPath seriesPath, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ImageSegmentModel>>([.. Items.OfType<ImageSegmentModel>().Where(s => s.Path.IsDescendantOf(seriesPath))]);

	public Task<IEnumerable<ImageSegmentModel>> FindImageSegmentsByChapterPaths(IEnumerable<BookPath> chapterPaths, CancellationToken cancellationToken = default) {
		var paths = chapterPaths.ToList();
		return Task.FromResult<IEnumerable<ImageSegmentModel>>(
			[.. Items.OfType<ImageSegmentModel>().Where(s => paths.Any(p => s.Path.IsDescendantOf(p)))]
		);
	}
}

public sealed class InMemorySeriesRepository(
	InMemoryVolumeRepository? volumes = null,
	InMemoryChapterRepository? chapters = null,
	InMemorySegmentRepository? segments = null) : InMemoryRepository<SeriesModel>, ISeriesRepository {
	private readonly InMemoryVolumeRepository? _volumes = volumes;
	private readonly InMemoryChapterRepository? _chapters = chapters;
	private readonly InMemorySegmentRepository? _segments = segments;

	public Task<SeriesModel?> FindOneByTitle(string title, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.FirstOrDefault(s => s.Title == title));

	public Task DeleteSubtreeAsync(Guid seriesId, BookPath path, CancellationToken cancellationToken = default) {
		Items.RemoveAll(s => s.Id == seriesId);
		_volumes?.Items.RemoveAll(v => v.Path.IsDescendantOf(path));
		_chapters?.Items.RemoveAll(c => c.Path.IsDescendantOf(path));
		_segments?.Items.RemoveAll(s => s.Path.IsDescendantOf(path));
		return Task.CompletedTask;
	}
}

public sealed class InMemoryVolumeRepository(
	InMemoryChapterRepository? chapters = null,
	InMemorySegmentRepository? segments = null) : InMemoryRepository<VolumeModel>, IVolumeRepository {
	private readonly InMemoryChapterRepository? _chapters = chapters;
	private readonly InMemorySegmentRepository? _segments = segments;

	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
		Task.FromResult<IEnumerable<VolumeModel>>([.. Items.Where(v => v.Path.GetSeriesId() == seriesId).OrderBy(v => v.Order)]);

	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
		Task.FromResult<IEnumerable<VolumeModel>>([.. Items.Where(v => v.Path.GetSeriesId() == seriesId).OrderBy(v => v.Order).Skip((pageIndex - 1) * pageSize).Take(pageSize)]);

	public Task<int> CountBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.Count(v => v.Path.GetSeriesId() == seriesId));

	public Task<VolumeModel?> FindBySeriesIdAndOrder(Guid seriesId, double order, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.FirstOrDefault(v => v.Path.GetSeriesId() == seriesId && v.Order == order));

	public Task MoveVolumeAsync(Guid volumeId, BookPath oldPath, BookPath newPath, double newOrder, CancellationToken cancellationToken = default) {
		var volume = Items.FirstOrDefault(v => v.Id == volumeId);
		if (volume is not null) {
			volume.Path = newPath;
			volume.Order = newOrder;
		}
		return Task.CompletedTask;
	}

	public Task DeleteSubtreeAsync(Guid volumeId, BookPath path, CancellationToken cancellationToken = default) {
		Items.RemoveAll(v => v.Id == volumeId);
		_chapters?.Items.RemoveAll(c => c.Path.IsDescendantOf(path));
		_segments?.Items.RemoveAll(s => s.Path.IsDescendantOf(path));
		return Task.CompletedTask;
	}
}

public sealed class InMemoryChapterRepository(InMemorySegmentRepository? segments = null) : InMemoryRepository<ChapterModel>, IChapterRepository {
	private readonly InMemorySegmentRepository? _segments = segments;

	public Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, CancellationToken cancellationToken = default) =>
		Task.FromResult<IEnumerable<ChapterModel>>([.. Items.Where(c => c.Path.GetVolumeId() == volumeId).OrderBy(c => c.Order)]);

	public Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
		Task.FromResult<IEnumerable<ChapterModel>>([.. Items.Where(c => c.Path.GetVolumeId() == volumeId).OrderBy(c => c.Order).Skip((pageIndex - 1) * pageSize).Take(pageSize)]);

	public Task<int> CountByVolumeId(Guid volumeId, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.Count(c => c.Path.GetVolumeId() == volumeId));

	public Task<IEnumerable<ChapterModel>> FindByVolumeIds(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) {
		var set = volumeIds.ToHashSet();
		return Task.FromResult<IEnumerable<ChapterModel>>([.. Items.Where(c => set.Contains(c.Path.GetVolumeId())).OrderBy(c => c.Path.GetVolumeId()).ThenBy(c => c.Order)]);
	}

	public Task<IEnumerable<ChapterModel>> FindByVolumeIdsWithContent(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) =>
		FindByVolumeIds(volumeIds, cancellationToken);

	public Task<ChapterModel?> FindByVolumeIdAndOrder(Guid volumeId, double order, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.FirstOrDefault(c => c.Path.GetVolumeId() == volumeId && c.Order == order));

	public Task MoveChapterAsync(Guid chapterId, BookPath oldPath, BookPath newPath, double newOrder, CancellationToken cancellationToken = default) {
		var chapter = Items.FirstOrDefault(c => c.Id == chapterId);
		if (chapter is not null) {
			chapter.Path = newPath;
			chapter.Order = newOrder;
		}
		return Task.CompletedTask;
	}

	public Task DeleteSubtreeAsync(Guid chapterId, BookPath path, CancellationToken cancellationToken = default) {
		Items.RemoveAll(c => c.Id == chapterId);
		_segments?.Items.RemoveAll(s => s.Path.IsDescendantOf(path));
		return Task.CompletedTask;
	}
}

public sealed class InMemoryAssetRepository : InMemoryRepository<AssetModel>, IAssetRepository {
	public Task<AssetModel?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.FirstOrDefault(a => a.FileHash == fileHash));

	public Task<AssetModel?> GetBySeriesAndFileHashAsync(Guid seriesId, string fileHash, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.FirstOrDefault(a => a.SeriesId == seriesId && a.FileHash == fileHash));

	public Task<IReadOnlyDictionary<Guid, AssetModel>> FindByIdsAsync(IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default) {
		var set = assetIds.ToHashSet();
		return Task.FromResult<IReadOnlyDictionary<Guid, AssetModel>>(Items.Where(a => set.Contains(a.Id)).ToDictionary(a => a.Id));
	}
}

public sealed class InMemorySourceMaterialRepository : InMemoryRepository<SourceMaterial>, ISourceMaterialRepository {
	public Task<IEnumerable<SourceMaterial>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
		Task.FromResult<IEnumerable<SourceMaterial>>([.. Items.Where(s => s.SeriesId == seriesId)]);
}

public sealed class NoOpUnitOfWork : IUnitOfWork {
	public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
	public void RegisterPostCommitAction(Func<Task> action) => action().GetAwaiter().GetResult();
}
