namespace Grimoire.Application.Extensions;

using Dto.Common;

public static class EnumerableExtensions {
	public static PagedResult<T> ToPagedList<T>(
		this IEnumerable<T> source,
		PaginationRequest request) {
		var enumerable = source as T[] ?? source.ToArray();
		var totalCount = enumerable.Length;
		var items = enumerable
			.Skip((request.PageIndex - 1) * request.PageSize)
			.Take(request.PageSize)
			.ToList();

		return new PagedResult<T>(items, totalCount, request.PageIndex, request.PageSize);
	}
}
