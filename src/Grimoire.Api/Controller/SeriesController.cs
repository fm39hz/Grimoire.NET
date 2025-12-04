namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Application.Mapper;
using Application.Service.Contract;
using Constant;
using Dto;
using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class SeriesController(ISeriesService service, IBookMapper mapper) : ControllerBase {
	[HttpGet("{id:guid}")]
	public async Task<IResult> FindOne(Guid id) {
		var series = await service.FindOne(id);
		return series is null? Results.NotFound() : Results.Ok(mapper.ToSeriesDto(series));
	}

	[HttpGet]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto? pagination) {
		var series = await service.FindAll();
		var dto = series.Select(mapper.ToSeriesDto);
		return Results.Ok(dto);
	}

	[HttpPost]
	public async Task<IResult> Create([FromBody] CreateSeriesRequestDto dto) {
		try {
			var createdSeries = await service.Create(dto);
			return Results.Created($"{createdSeries.Id}", mapper.ToSeriesDto(createdSeries));
		}
		catch (UniqueConstraintException e) {
			return Results.Conflict($"{e.ConstraintName} existed at {e.SchemaQualifiedTableName}");
		}
	}

	[HttpPut("{id:guid}")]
	public async Task<IResult> Update(Guid id, [FromBody] UpdateSeriesRequestDto dto) {
		var updatedSeries = await service.Update(id, dto);
		return Results.Ok(mapper.ToSeriesDto(updatedSeries));
	}

	[HttpDelete("{id:guid}")]
	public async Task<IResult> Delete(Guid id) {
		var result = await service.Delete(id);
		return Results.Ok(result);
	}
}
