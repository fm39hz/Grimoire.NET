namespace Grimoire.Application.Extensions;

using Dto.Common;

public static class EnumerableExtensions {
	public static PagedResult<T> ToPagedList<T>(
		this IEnumerable<T> source,
		PaginationRequest request,
		int totalCount) {
		var items = source
			.Skip((request.PageIndex - 1) * request.PageSize)
			.Take(request.PageSize)
			.ToList();

		return new PagedResult<T>(items, totalCount, request.PageIndex, request.PageSize);
	}
}
