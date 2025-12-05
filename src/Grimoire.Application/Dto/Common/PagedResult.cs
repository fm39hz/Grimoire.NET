namespace Grimoire.Application.Dto.Common;

public class PagedResult<T>(List<T> items, int count, int pageIndex, int pageSize) {
	public List<T> Items { get; init; } = items;
	public int TotalCount { get; init; } = count;
	public int PageIndex { get; init; } = pageIndex;
	public int PageSize { get; init; } = pageSize;
	public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
