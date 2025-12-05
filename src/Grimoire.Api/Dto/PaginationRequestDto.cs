namespace Grimoire.Api.Dto;

using System.ComponentModel;
using Grimoire.Application.Dto.Common;
using Microsoft.AspNetCore.Mvc;

public record PaginationRequestDto {
	[FromQuery(Name = "pageIndex")]
	[DefaultValue(1)]
	public int PageIndex { get; set; } = 1;

	[FromQuery(Name = "pageSize")]
	[DefaultValue(10)]
	public int PageSize { get; set; } = 10;

	[FromQuery(Name = "sortBy")] 
	public string? SortBy { get; set; }

	[FromQuery(Name = "sortDescending")] 
	public bool SortDescending { get; set; }

	public PaginationRequest ToApplicationDto() => new() {
		PageIndex = PageIndex,
		PageSize = PageSize,
		SortBy = SortBy,
		SortDescending = SortDescending
	};
}
