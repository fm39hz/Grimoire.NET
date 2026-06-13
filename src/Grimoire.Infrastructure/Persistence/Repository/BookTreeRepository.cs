namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.EntityFrameworkCore;

public sealed class BookTreeRepository(ApplicationDbContext context)
	: CrudRepository<BookNodeModel>(context), IBookTreeRepository {
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

	public async Task<BookNodeModel?> FindChildByOrder(Guid? parentId, double order, CancellationToken cancellationToken = default) =>
		await Entities
			.AsNoTracking()
			.FirstOrDefaultAsync(n => n.ParentId == parentId && n.Order == order, cancellationToken);

	public async Task<IReadOnlyList<BookNodeModel>> FindSeriesTree(Guid seriesId, CancellationToken cancellationToken = default) {
		LTree seriesPath = "n" + seriesId.ToString("N");
		return await Entities
			.AsNoTracking()
			.Where(n => ((LTree)n.Path).IsDescendantOf(seriesPath))
			.OrderBy(n => n.Type)
			.ThenBy(n => n.Order)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<BookNodeModel>> FindSubtree(Guid nodeId, CancellationToken cancellationToken = default) {
		var node = await Entities.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);
		if (node is null) {
			return [];
		}

		LTree parentPath = node.Path;
		return await Entities
			.AsNoTracking()
			.Where(n => ((LTree)n.Path).IsDescendantOf(parentPath))
			.OrderBy(n => n.Type)
			.ThenBy(n => n.Order)
			.ToListAsync(cancellationToken);
	}

	public async Task UpdateSubtreePaths(Guid nodeId, string oldPath, string newPath, CancellationToken cancellationToken = default) {
		LTree oldLTreePath = oldPath;
		var oldPathLength = oldPath.Split('.').Length;

		await Entities
			.Where(n => n.Id != nodeId && ((LTree)n.Path).IsDescendantOf(oldLTreePath))
			.ExecuteUpdateAsync(s => s.SetProperty(
				n => n.Path,
				n => newPath + (string)((LTree)n.Path).Subpath(oldPathLength)
			), cancellationToken);
	}
}
