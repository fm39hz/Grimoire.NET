namespace Grimoire.Domain.Common.Repository;

public interface IRepository<T> {
	public Task<T?> FindOne(Guid id);
	public Task<IEnumerable<T>> FindAll();
	public Task<T> Create(T entity);
	public Task<T> Update(T entity);
	public Task<int> Delete(Guid id);
}
