namespace Grimoire.Tests.TestInfrastructure;

using Grimoire.Domain.Common;
using Grimoire.Domain.Common.Repository;
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
		return Task.FromResult<IEnumerable<T>>(Items.Where(i => set.Contains(i.Id)).ToList());
	}

	public virtual Task<int> Delete(Guid id, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.RemoveAll(i => i.Id == id));

	public virtual Task<int> DeleteMany(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) {
		var set = ids.ToHashSet();
		return Task.FromResult(Items.RemoveAll(i => set.Contains(i.Id)));
	}
}

public sealed class InMemoryBookTreeRepository : InMemoryRepository<BookNodeModel>, IBookTreeRepository {
	public Task<IEnumerable<BookNodeModel>> FindChildren(Guid? parentId, CancellationToken cancellationToken = default) =>
		Task.FromResult<IEnumerable<BookNodeModel>>(Items.Where(n => n.ParentId == parentId).OrderBy(n => n.Order).ToList());

	public Task<IEnumerable<BookNodeModel>> FindChildren(Guid? parentId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
		Task.FromResult<IEnumerable<BookNodeModel>>(Items
			.Where(n => n.ParentId == parentId)
			.OrderBy(n => n.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToList());

	public Task<int> CountChildren(Guid? parentId, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.Count(n => n.ParentId == parentId));

	public Task<BookNodeModel?> FindChildByOrder(Guid? parentId, double order, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.FirstOrDefault(n => n.ParentId == parentId && n.Order == order));

	public Task<IReadOnlyList<BookNodeModel>> FindSeriesTree(Guid seriesId, CancellationToken cancellationToken = default) {
		var series = Items.Where(n => n.Id == seriesId).ToList();
		var volumes = Items.Where(n => n.ParentId == seriesId).OrderBy(n => n.Order).ToList();
		var volumeIds = volumes.Select(v => v.Id).ToHashSet();
		var chapters = Items.Where(n => n.ParentId is not null && volumeIds.Contains(n.ParentId.Value)).OrderBy(n => n.Order).ToList();
		return Task.FromResult<IReadOnlyList<BookNodeModel>>([.. series, .. volumes, .. chapters]);
	}

	public Task<IReadOnlyList<BookNodeModel>> FindSubtree(Guid nodeId, CancellationToken cancellationToken = default) {
		var result = new List<BookNodeModel>();
		var pending = new Queue<Guid>();
		pending.Enqueue(nodeId);
		while (pending.Count > 0) {
			var id = pending.Dequeue();
			var node = Items.FirstOrDefault(n => n.Id == id);
			if (node is null) continue;
			result.Add(node);
			foreach (var child in Items.Where(n => n.ParentId == id)) {
				pending.Enqueue(child.Id);
			}
		}

		return Task.FromResult<IReadOnlyList<BookNodeModel>>(result);
	}

	public Task UpdateSubtreePaths(Guid nodeId, string oldPath, string newPath, CancellationToken cancellationToken = default) {
		foreach (var node in Items) {
			if (node.Id != nodeId && node.Path.StartsWith(oldPath)) {
				node.Path = newPath + node.Path[oldPath.Length..];
			}
		}
		return Task.CompletedTask;
	}
}

public sealed class InMemorySeriesRepository : InMemoryRepository<SeriesModel>, ISeriesRepository {
	public Task<SeriesModel?> FindOneByTitle(string title, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.FirstOrDefault(s => s.Title == title));
}

public sealed class InMemoryVolumeRepository : InMemoryRepository<VolumeModel>, IVolumeRepository {
	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
		Task.FromResult<IEnumerable<VolumeModel>>(Items.Where(v => v.SeriesId == seriesId).OrderBy(v => v.Order).ToList());

	public Task<IEnumerable<VolumeModel>> FindBySeriesId(Guid seriesId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
		Task.FromResult<IEnumerable<VolumeModel>>(Items.Where(v => v.SeriesId == seriesId).OrderBy(v => v.Order).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList());

	public Task<int> CountBySeriesId(Guid seriesId, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.Count(v => v.SeriesId == seriesId));

	public Task<VolumeModel?> FindBySeriesIdAndOrder(Guid seriesId, double order, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.FirstOrDefault(v => v.SeriesId == seriesId && v.Order == order));
}

public sealed class InMemoryChapterRepository : InMemoryRepository<ChapterModel>, IChapterRepository {
	public Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, CancellationToken cancellationToken = default) =>
		Task.FromResult<IEnumerable<ChapterModel>>(Items.Where(c => c.VolumeId == volumeId).OrderBy(c => c.Order).ToList());

	public Task<IEnumerable<ChapterModel>> FindByVolumeId(Guid volumeId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
		Task.FromResult<IEnumerable<ChapterModel>>(Items.Where(c => c.VolumeId == volumeId).OrderBy(c => c.Order).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList());

	public Task<int> CountByVolumeId(Guid volumeId, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.Count(c => c.VolumeId == volumeId));

	public Task<IEnumerable<ChapterModel>> FindByVolumeIds(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) {
		var set = volumeIds.ToHashSet();
		return Task.FromResult<IEnumerable<ChapterModel>>(Items.Where(c => set.Contains(c.VolumeId)).OrderBy(c => c.VolumeId).ThenBy(c => c.Order).ToList());
	}

	public Task<IEnumerable<ChapterModel>> FindByVolumeIdsWithContent(IEnumerable<Guid> volumeIds, CancellationToken cancellationToken = default) =>
		FindByVolumeIds(volumeIds, cancellationToken);

	public Task<ChapterModel?> FindByVolumeIdAndOrder(Guid volumeId, double order, CancellationToken cancellationToken = default) =>
		Task.FromResult(Items.FirstOrDefault(c => c.VolumeId == volumeId && c.Order == order));
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
		Task.FromResult<IEnumerable<SourceMaterial>>(Items.Where(s => s.SeriesId == seriesId).ToList());
}

public sealed class NoOpUnitOfWork : IUnitOfWork {
	public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}
