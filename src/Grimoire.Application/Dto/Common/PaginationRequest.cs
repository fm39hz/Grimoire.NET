namespace Grimoire.Application.Dto.Common;

public class PaginationRequest {
	public int PageIndex {
		get;
		init => field = value < 1 ? 1 : value;
	} = 1;

	public int PageSize {
		get;
		init => field = value switch {
			< 1 => 10,
			> 100 => 100,
			_ => value
		};
	} = 10;

	public string? SortBy { get; set; }
	public bool SortDescending { get; set; }
}
