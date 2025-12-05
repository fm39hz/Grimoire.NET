namespace Grimoire.Application.Dto.Common;

public class PaginationRequest {
	private int _pageSize = 10;
	private int _pageIndex = 1;

	public int PageIndex {
		get => _pageIndex;
		set => _pageIndex = value < 1 ? 1 : value;
	}

	public int PageSize {
		get => _pageSize;
		set => _pageSize = value switch {
			< 1 => 10,
			> 100 => 100,
			_ => value
		};
	}

	public string? SortBy { get; set; }
	public bool SortDescending { get; set; }
}
