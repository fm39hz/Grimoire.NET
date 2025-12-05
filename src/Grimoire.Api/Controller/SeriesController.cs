namespace Grimoire.Api.Controller;

using Application.Common;
using Application.Dto.Book;
using Application.Dto.Common;
using Application.Mapper;
using Application.Service.Contract;
using Constant;
using Dto;
using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class SeriesController(ISeriesService service, IBookMapper mapper) : ControllerBase {
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(SeriesResponseDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IResult> FindOne(string id) {
		var guid = PrefixedId.ToGuid(id);
		var series = await service.FindOne(guid);
		return series is null? Results.NotFound() : Results.Ok(mapper.ToSeriesDto(series));
	}

	[HttpGet]
	[ProducesResponseType(typeof(PagedResult<SeriesResponseDto>), 200)]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto? pagination) {
		if (pagination == null) {
			var series = await service.FindAll();
			var dto = series.Select(mapper.ToSeriesDto);
			return Results.Ok(dto);
		}

		var pagedSeries = await service.FindAll(pagination.ToApplicationDto());
		var pagedDto = new PagedResult<SeriesResponseDto>(
			pagedSeries.Items.Select(mapper.ToSeriesDto).ToList(),
			pagedSeries.TotalCount,
			pagedSeries.PageIndex,
			pagedSeries.PageSize
			);
		return Results.Ok(pagedDto);
	}

	[HttpPost]
	[ProducesResponseType(typeof(SeriesResponseDto), 201)]
	[ProducesResponseType(409)]
	public async Task<IResult> Create([FromBody] CreateSeriesRequestDto dto) {
		try {
			var createdSeries = await service.Create(dto);
			var responseDto = mapper.ToSeriesDto(createdSeries);
			return Results.Created($"{responseDto.Id}", responseDto);
		}
		catch (UniqueConstraintException e) {
			return Results.Conflict($"{e.ConstraintName} existed at {e.SchemaQualifiedTableName}");
		}
	}

	[HttpPatch("{id}")]
	[ProducesResponseType(typeof(SeriesResponseDto), 200)]
	public async Task<IResult> Update(string id, [FromBody] UpdateSeriesRequestDto dto) {
		var guid = PrefixedId.ToGuid(id);
		var updatedSeries = await service.Update(guid, dto);
		return Results.Ok(mapper.ToSeriesDto(updatedSeries));
	}

	[HttpDelete("{id}")]
	[ProducesResponseType(typeof(bool), 200)]
	public async Task<IResult> Delete(string id) {
		var guid = PrefixedId.ToGuid(id);
		var result = await service.Delete(guid);
		return Results.Ok(result);
	}

	[HttpGet("{id}/volumes")]
	[ProducesResponseType(typeof(IEnumerable<VolumeResponseDto>), 200)]
	public async Task<IResult> GetVolumes(string id, [FromQuery] PaginationRequestDto? pagination) {
		var guid = PrefixedId.ToGuid(id);
		if (pagination == null) {
			var series = await service.FindAllVolumes(guid);
			var dto = series.Select(mapper.ToVolumeDto);
			return Results.Ok(dto);
		}

		var pagedVolumes = await service.FindAllVolumes(guid, pagination.ToApplicationDto());
		var pagedDto = new PagedResult<VolumeResponseDto>(
			pagedVolumes.Items.Select(mapper.ToVolumeDto).ToList(),
			pagedVolumes.TotalCount,
			pagedVolumes.PageIndex,
			pagedVolumes.PageSize
			);
		return Results.Ok(pagedDto);
	}
}
