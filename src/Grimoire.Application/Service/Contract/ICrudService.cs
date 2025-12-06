namespace Grimoire.Application.Service.Contract;

using Common;
using Domain.Entity;
using Dto.Common;

/// <summary>
///     An interface that declare Crud actions service
/// </summary>
/// <typeparam name="TModel">Target model</typeparam>
/// <typeparam name="TCreateDto">Create DTO</typeparam>
/// <typeparam name="TUpdateDto">Update DTO</typeparam>
public interface ICrudService<TModel, in TCreateDto, in TUpdateDto>
	where TModel : IModel {
	/// <summary>
	///     Find one entity with id in database
	/// </summary>
	/// <param name="id">Id of model</param>
	/// <returns>Matched model</returns>
	public Task<TModel?> FindOne(Guid id);

	/// <summary>
	///     Find all entity in database
	/// </summary>
	/// <returns>All matched model</returns>
	public Task<IEnumerable<TModel>> FindAll();

	/// <summary>
	///     Find all entity in database with pagination
	/// </summary>
	/// <param name="request">Pagination parameters</param>
	/// <returns>Paged result</returns>
	public Task<PagedResult<TModel>> FindAll(PaginationRequest request);

	/// <summary>
	///     Create new entity in database
	/// </summary>
	/// <param name="dto">the entity value</param>
	/// <returns>Newly created model</returns>
	public Task<TModel> Create(TCreateDto dto);

	/// <summary>
	///     Update one specify entity
	/// </summary>
	/// <param name="id">Id of model</param>
	/// <param name="dto">the entity value</param>
	/// <returns>Updated model</returns>
	public Task<TModel> Update(Guid id, TUpdateDto dto);

	/// <summary>
	///     Delete one entity that has id
	/// </summary>
	/// <param name="id">Id of model</param>
	/// <returns>number of deleted model</returns>
	public Task<int> Delete(Guid id);
}
