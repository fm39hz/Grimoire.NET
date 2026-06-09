namespace Grimoire.Application.Service.Contract;

using System.Threading;
using Domain.Common;
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
	public Task<TModel?> FindOne(Guid id, CancellationToken cancellationToken = default);

	/// <summary>
	///     Find all entity in database with pagination
	/// </summary>
	/// <param name="request">Pagination parameters</param>
	/// <returns>Paged result</returns>
	public Task<PagedResult<TModel>> FindAll(PaginationRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	///     Create new entity in database
	/// </summary>
	/// <param name="dto">the entity value</param>
	/// <returns>Newly created model</returns>
	public Task<TModel> Create(TCreateDto dto, CancellationToken cancellationToken = default);

	/// <summary>
	///     Update one specify entity
	/// </summary>
	/// <param name="id">Id of model</param>
	/// <param name="dto">the entity value</param>
	/// <returns>Updated model</returns>
	public Task<TModel> Update(Guid id, TUpdateDto dto, CancellationToken cancellationToken = default);

	/// <summary>
	///     Delete one entity that has id
	/// </summary>
	/// <param name="id">Id of model</param>
	/// <returns>number of deleted model</returns>
	public Task<int> Delete(Guid id, CancellationToken cancellationToken = default);
}
