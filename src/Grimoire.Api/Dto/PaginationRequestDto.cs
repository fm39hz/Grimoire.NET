namespace Grimoire.Api.Dto;

using System.ComponentModel;
using Application.Dto.Common;
using Microsoft.AspNetCore.Mvc;

public record PaginationRequestDto {
	[FromQuery(Name = "pageIndex")]
	[DefaultValue(1)]
	public int PageIndex { get; init; }

	[FromQuery(Name = "pageSize")]
	[DefaultValue(10)]
	public int PageSize { get; init; }

	[FromQuery(Name = "sortBy")] public string? SortBy { get; init; }

	[FromQuery(Name = "sortDescending")] public bool SortDescending { get; init; }

	public PaginationRequest ToApplicationDto() => new() {
		PageIndex = PageIndex,
		PageSize = PageSize,
		SortBy = SortBy,
		SortDescending = SortDescending
	};
}
