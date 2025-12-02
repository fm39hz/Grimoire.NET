namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Application.Dto.Common;
using Application.Service.Contract;
using Domain.Constant;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class SeriesController(ISeriesService service)
	: ControllerBase {
	[HttpGet("{id:guid}")]
	public async Task<IResult> FindOne(Guid id) {
		var series = await service.FindOne(id);
		return series is null? Results.NotFound() : Results.Ok(new SeriesResponseDto(series));
	}

	[HttpGet]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto? pagination) {
		var series = await service.FindAll();
		var dto = series.Select(s => new SeriesResponseDto(s));
		return Results.Ok(dto);
	}

	[HttpPost]
	public async Task<IResult> Create([FromBody] SeriesRequestDto entity) {
		var createdSeries = await service.Create(entity.ToModel());
		return Results.Created($"/api/v1/series/{createdSeries.Id}", new SeriesResponseDto(createdSeries));
	}

	[HttpPut("{id:guid}")]
	public async Task<IResult> Update(Guid id, [FromBody] SeriesRequestDto entity) {
		var updatedSeries = await service.Update(id, entity.ToModel());
		return Results.Ok(new SeriesResponseDto(updatedSeries));
	}

	[HttpDelete("{id:guid}")]
	public async Task<IResult> Delete(Guid id) {
		var result = await service.Delete(id);
		return Results.Ok(result);
	}
}
