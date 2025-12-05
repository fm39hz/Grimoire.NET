namespace Grimoire.Api.Dto;

using System.ComponentModel;
using Application.Dto.Common;
using Microsoft.AspNetCore.Mvc;

public record PaginationRequestDto {
	[FromQuery(Name = "pageIndex")]
	[DefaultValue(1)]
	private int PageIndex { get; } = 1;

	[FromQuery(Name = "pageSize")]
	[DefaultValue(10)]
	private int PageSize { get; } = 10;

	[FromQuery(Name = "sortBy")] public string? SortBy { get; set; }

	[FromQuery(Name = "sortDescending")] public bool SortDescending { get; set; }

	public PaginationRequest ToApplicationDto() => new() {
		PageIndex = PageIndex, PageSize = PageSize, SortBy = SortBy, SortDescending = SortDescending
	};
}
