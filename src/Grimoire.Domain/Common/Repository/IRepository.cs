namespace Grimoire.Domain.Common.Repository;

public interface IRepository<T> {
	public Task<T?> FindOne(Guid id);
	public Task<IEnumerable<T>> FindAll(int pageIndex, int pageSize);
	public Task<int> CountAll();
	public Task<T> Create(T entity);
	public Task<IEnumerable<T>> CreateBulk(IEnumerable<T> entities);
	public Task<T> Update(T entity);
	public Task<int> Delete(Guid id);
}
