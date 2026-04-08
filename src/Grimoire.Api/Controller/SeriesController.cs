namespace Grimoire.Api.Controller;

using Application.Common;
using Application.Dto.Book;
using Application.Mapper;
using Application.Service.Contract;
using Constant;
using Dto;
using EntityFramework.Exceptions.Common;
using Grimoire.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using static Application.Common.SegmentMarkdownConverter;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class SeriesController(ISeriesService service, IBookMapper mapper) : ControllerBase {
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(SeriesResponseDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IResult> FindOne(string id, [FromQuery] bool? timestamp = false, [FromQuery] bool? markdown = false) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		var series = await service.FindOne(guid);
		if (series is null) {
			return Results.NotFound();
		}

		var dto = mapper.ToSeriesDto(series);
		if (timestamp != true) {
			dto.CreatedAt = null;
			dto.UpdatedAt = null;
		}
		if (markdown == true) {
			dto.Markdown = ConvertTextSegmentsToMarkdown(dto.Metadata.Description);
		}
		return Results.Ok(dto);
	}

	[HttpGet]
	[ProducesResponseType(typeof(PagedResult<SeriesResponseDto>), 200)]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto? pagination, [FromQuery] bool? markdown = false) {
		if (pagination == null) {
			var series = await service.FindAll();
			var dtos = series.Select(s => {
				var dto = mapper.ToSeriesDto(s);
				if (markdown == true) {
					dto.Markdown = ConvertTextSegmentsToMarkdown(dto.Metadata.Description);
				}
				return dto;
			});
			return Results.Ok(dtos);
		}

		var pagedSeries = await service.FindAll(pagination.ToApplicationDto());
		var items = pagedSeries.Items.Select(s => {
			var dto = mapper.ToSeriesDto(s);
			if (markdown == true) {
				dto.Markdown = ConvertTextSegmentsToMarkdown(dto.Metadata.Description);
			}
			return dto;
		}).ToList();
		var pagedDto = new PagedResult<SeriesResponseDto>(
			items,
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
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		var updatedSeries = await service.Update(guid, dto);
		return Results.Ok(mapper.ToSeriesDto(updatedSeries));
	}

	[HttpDelete("{id}")]
	[ProducesResponseType(typeof(bool), 200)]
	public async Task<IResult> Delete(string id) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		var result = await service.Delete(guid);
		return Results.Ok(result);
	}

	[HttpGet("{id}/volumes")]
	[ProducesResponseType(typeof(IEnumerable<VolumeResponseDto>), 200)]
	public async Task<IResult> GetVolumes(string id, [FromQuery] PaginationRequestDto? pagination) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		if (pagination == null) {
			var series = await service.FindAllVolumes(guid);
			var dto = series.Select(mapper.ToVolumeDto);
			return Results.Ok(dto);
		}

		var pagedVolumes = await service.FindAllVolumes(guid, pagination.ToApplicationDto());
		var pagedDto = new PagedResult<VolumeResponseDto>(
			[.. pagedVolumes.Items.Select(mapper.ToVolumeDto)],
			pagedVolumes.TotalCount,
			pagedVolumes.PageIndex,
			pagedVolumes.PageSize
			);
		return Results.Ok(pagedDto);
	}

	// [HttpPost("import/epub")]
	// [Consumes("multipart/form-data")]
	// [ProducesResponseType(typeof(SeriesResponseDto), 201)]
	// [ProducesResponseType(400)]
	// [ProducesResponseType(500)]
	// public async Task<IResult> ImportEpub(
	// 	IFormFile file,
	// 	[FromQuery] string? existingSeriesId) {
	// 	if (file == null || file.Length == 0) {
	// 		return Results.BadRequest("EPUB file is required");
	// 	}
	//
	// 	if (!file.FileName.EndsWith(".epub", StringComparison.OrdinalIgnoreCase)) {
	// 		return Results.BadRequest("File must be an EPUB file (.epub)");
	// 	}
	//
	// 	// Build options - auto-detect based on existingSeriesId
	// 	var importOptions = new ImportEpubRequestDto {
	// 		ExistingSeriesId = existingSeriesId
	// 	};
	//
	// 	await using var stream = file.OpenReadStream();
	// 	var series = await service.ImportFromEpubAsync(stream, importOptions);
	// 	var responseDto = mapper.ToSeriesDto(series);
	// 	return Results.Created($"{responseDto.Id}", responseDto);
	// }
}
