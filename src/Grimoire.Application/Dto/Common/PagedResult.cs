namespace Grimoire.Application.Dto.Common;

public class PagedResult<T> {
	public List<T> Items { get; set; }
	public int TotalCount { get; set; }
	public int PageIndex { get; set; }
	public int PageSize { get; set; }
	public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

	public PagedResult(List<T> items, int count, int pageIndex, int pageSize) {
		Items = items;
		TotalCount = count;
		PageIndex = pageIndex;
		PageSize = pageSize;
	}
}
