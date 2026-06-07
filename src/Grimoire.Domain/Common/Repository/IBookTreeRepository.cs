namespace Grimoire.Domain.Common.Repository;

using System.Threading;
using Entity.Book;

public interface IBookTreeRepository : IRepository<BookNodeModel> {
	public Task<BookNodeModel?> FindOneTracked(Guid id, CancellationToken cancellationToken = default);
	public Task<IEnumerable<BookNodeModel>> FindChildren(Guid? parentId, CancellationToken cancellationToken = default);
	public Task<IEnumerable<BookNodeModel>> FindChildren(Guid? parentId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
	public Task<int> CountChildren(Guid? parentId, CancellationToken cancellationToken = default);
	public Task<BookNodeModel?> FindChildByOrder(Guid? parentId, float order, CancellationToken cancellationToken = default);
	public Task<IReadOnlyList<BookNodeModel>> FindSeriesTree(Guid seriesId, CancellationToken cancellationToken = default);
	public Task<IReadOnlyList<BookNodeModel>> FindSubtree(Guid nodeId, CancellationToken cancellationToken = default);
	public Task DeleteMany(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
