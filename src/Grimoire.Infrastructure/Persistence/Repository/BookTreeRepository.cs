namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class BookTreeRepository(ApplicationDbContext context)
	: CrudRepository<BookNodeModel>(context), IBookTreeRepository {
	public async Task<BookNodeModel?> FindOneTracked(Guid id, CancellationToken cancellationToken = default) =>
		await Entities.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

	public async Task<IEnumerable<BookNodeModel>> FindChildren(Guid? parentId, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(n => n.ParentId == parentId)
			.OrderBy(n => n.Order)
			.ToListAsync(cancellationToken);

	public async Task<IEnumerable<BookNodeModel>> FindChildren(Guid? parentId, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(n => n.ParentId == parentId)
			.OrderBy(n => n.Order)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(cancellationToken);

	public async Task<int> CountChildren(Guid? parentId, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.Where(n => n.ParentId == parentId)
			.CountAsync(cancellationToken);

	public async Task<BookNodeModel?> FindChildByOrder(Guid? parentId, float order, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.FirstOrDefaultAsync(n => n.ParentId == parentId && n.Order == order, cancellationToken);

	public async Task<IReadOnlyList<BookNodeModel>> FindSeriesTree(Guid seriesId, CancellationToken cancellationToken = default) {
		var series = await Entities.AsNoTracking().FirstOrDefaultAsync(n => n.Id == seriesId, cancellationToken);
		if (series is null) {
			return [];
		}

		var volumes = await Entities
			.AsNoTracking()
			.Where(n => n.ParentId == seriesId)
			.OrderBy(n => n.Order)
			.ToListAsync(cancellationToken);
		var volumeIds = volumes.Select(v => v.Id).ToList();
		var chapters = volumeIds.Count == 0
			? []
			: await Entities
				.AsNoTracking()
				.Where(n => n.ParentId != null && volumeIds.Contains(n.ParentId.Value))
				.OrderBy(n => n.ParentId)
				.ThenBy(n => n.Order)
				.ToListAsync(cancellationToken);

		return [series, .. volumes, .. chapters];
	}

	public async Task<IReadOnlyList<BookNodeModel>> FindSubtree(Guid nodeId, CancellationToken cancellationToken = default) {
		var result = new List<BookNodeModel>();
		var pending = new Queue<Guid>();
		pending.Enqueue(nodeId);

		while (pending.Count > 0) {
			var id = pending.Dequeue();
			var node = await Entities.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
			if (node is null) {
				continue;
			}

			result.Add(node);
			var children = await Entities
				.AsNoTracking()
				.Where(n => n.ParentId == id)
				.Select(n => n.Id)
				.ToListAsync(cancellationToken);
			foreach (var childId in children) {
				pending.Enqueue(childId);
			}
		}

		return result;
	}

	public async Task DeleteMany(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
		await Entities.Where(n => ids.Contains(n.Id)).ExecuteDeleteAsync(cancellationToken);
}
