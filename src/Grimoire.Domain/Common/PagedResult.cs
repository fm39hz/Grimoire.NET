namespace Grimoire.Domain.Common;

public class PagedResult<T>(List<T> items, int totalCount, int pageIndex, int pageSize) {
	public List<T> Items { get; init; } = items;
	public int TotalCount { get; init; } = totalCount;
	public int PageIndex { get; init; } = pageIndex;
	public int PageSize { get; init; } = pageSize;
	public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
	public bool HasPreviousPage => PageIndex > 1;
	public bool HasNextPage => PageIndex < TotalPages;
}
