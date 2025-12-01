namespace Grimoire.Application.Service.Contract;

using Domain.Entity;

/// <summary>
///     An interface that declare Crud actions service
/// </summary>
/// <typeparam name="T">Target model</typeparam>
public interface ICrudService<T> where T : IModel {
	/// <summary>
	///     Find one entity with id in database
	/// </summary>
	/// <param name="id">Id of model</param>
	/// <returns>Matched model</returns>
	public Task<T?> FindOne(Guid id);

	/// <summary>
	///     Find all entity in database
	/// </summary>
	/// <returns>All matched model</returns>
	public Task<IEnumerable<T>> FindAll();

	/// <summary>
	///     Create new entity in database
	/// </summary>
	/// <param name="entity">the entity value</param>
	/// <returns>Newly created model</returns>
	public Task<T> Create(T entity);

	/// <summary>
	///     Update one specify entity
	/// </summary>
	/// <param name="id">Id of model</param>
	/// <param name="entity">the entity value</param>
	/// <returns>Updated model</returns>
	public Task<T> Update(Guid id, T entity);

	/// <summary>
	///     Delete one entity that has id
	/// </summary>
	/// <param name="id">Id of model</param>
	/// <returns>number of deleted model</returns>
	public Task<int> Delete(Guid id);
}
