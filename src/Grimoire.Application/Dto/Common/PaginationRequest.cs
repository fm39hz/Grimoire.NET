namespace Grimoire.Application.Dto.Common;

public class PaginationRequest {
	public int PageIndex {
		get;
		init => field = value < PaginationDefaults.PageIndexMin
			? PaginationDefaults.PageIndexDefault
			: value;
	} = PaginationDefaults.PageIndexDefault;

	public int PageSize {
		get;
		init => field = value switch {
			< PaginationDefaults.PageSizeMin => PaginationDefaults.PageSizeDefault,
			> PaginationDefaults.PageSizeMax => PaginationDefaults.PageSizeMax,
			_ => value
		};
	} = PaginationDefaults.PageSizeDefault;

	public string? SortBy { get; set; }
	public bool SortDescending { get; set; }
}
