namespace Grimoire.Domain.Common.Repository;

using System.Threading;

public interface IRepository<T> {
	public Task<T?> FindOne(Guid id, CancellationToken cancellationToken = default);
	public Task<T?> FindOneTracked(Guid id, CancellationToken cancellationToken = default);
	public Task<PagedResult<T>> FindAll(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
	public Task<T> Create(T entity, CancellationToken cancellationToken = default);
	public Task<IEnumerable<T>> CreateBulk(IEnumerable<T> entities, CancellationToken cancellationToken = default);
	public Task<T> Update(T entity, CancellationToken cancellationToken = default);
	public Task<IEnumerable<T>> UpdateBulk(IEnumerable<T> entities, CancellationToken cancellationToken = default);
	public Task<IEnumerable<T>> FindByIds(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
	public Task<int> Delete(Guid id, CancellationToken cancellationToken = default);
	public Task<int> DeleteMany(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}


