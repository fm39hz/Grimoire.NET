namespace Grimoire.Application.Dto.Common;

public class PaginationRequest {
	private readonly int _pageIndex = 1;
	private readonly int _pageSize = 10;

	public int PageIndex {
		get => _pageIndex;
		init => _pageIndex = value < 1? 1 : value;
	}

	public int PageSize {
		get => _pageSize;
		init => _pageSize = value switch {
			< 1 => 10,
			> 100 => 100,
			_ => value
		};
	}

	public string? SortBy { get; set; }
	public bool SortDescending { get; set; }
}
