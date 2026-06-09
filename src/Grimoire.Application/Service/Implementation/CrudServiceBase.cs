namespace Grimoire.Application.Service.Implementation;

using System.Threading;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity;
using Dto.Common;

/// <summary>
///     Base class for CRUD services with common pagination logic
/// </summary>
public abstract class CrudServiceBase<TModel> where TModel : class, IModel {
	/// <summary>
	///     Helper method to create paginated results
	/// </summary>
	protected static async Task<PagedResult<TModel>> GetPagedResultAsync(IRepository<TModel> repository,
		PaginationRequest request, CancellationToken cancellationToken = default) {
		var items = await repository.FindAll(request.PageIndex, request.PageSize, cancellationToken);
		return items;
	}

	/// <summary>
	///     Helper method to create paginated results with custom item fetcher and counter
	/// </summary>
	protected static async Task<PagedResult<T>> GetPagedResultAsync<T>(
		Func<Task<IEnumerable<T>>> itemFetcher,
		Func<Task<int>> countFetcher,
		PaginationRequest request, CancellationToken cancellationToken = default) where T : class {
		var items = await itemFetcher();
		var totalCount = await countFetcher();
		return new PagedResult<T>([.. items], totalCount, request.PageIndex, request.PageSize);
	}
}
