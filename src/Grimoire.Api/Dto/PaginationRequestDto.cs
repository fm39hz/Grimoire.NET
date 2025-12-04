namespace Grimoire.Api.Dto;

using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

public record PaginationRequestDto {
	[FromQuery(Name = "page")]
	[DefaultValue(1)]
	public int Page { get; set; } = 1;

	[FromQuery(Name = "pageSize")]
	[DefaultValue(10)]
	public int PageSize { get; set; } = 10;

	[FromQuery(Name = "sortBy")] public string? SortBy { get; set; }

	[FromQuery(Name = "sortDescending")] public bool SortDescending { get; set; }

	[FromQuery(Name = "skip")] public int Skip => (Page - 1) * PageSize;

	public void Validate() {
		if (Page < 1) {
			Page = 1;
		}

		if (PageSize < 1) {
			PageSize = 10;
		}


		if (PageSize > 100) {
			PageSize = 100;
		}
	}
}
