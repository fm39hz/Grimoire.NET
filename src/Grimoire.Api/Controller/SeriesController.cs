namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Application.Dto.Book.Tree;
using Application.Mapper;
using Application.Service.Contract;
using Application.Service.Strategy;
using Constant;
using Domain.Common;
using Domain.Exception;
using Dto;
using Infrastructure.Export.Common;
using System.Threading;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class SeriesController(
	ISeriesService service,
	ISeriesSyncService syncService,
	IBookTreeService bookTreeService,
	IBookMapper mapper,
	ISectionRendererFactory rendererFactory) : ControllerBase {
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(SeriesResponseDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IResult> FindOne(string id, CancellationToken cancellationToken, [FromQuery] bool? timestamp = false) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		var series = await service.FindOne(guid, cancellationToken);
		if (series is null) {
			return Results.NotFound();
		}

		var dto = mapper.ToSeriesDto(series);
		if (timestamp == true) {
			return Results.Ok(dto);
		}

		dto.CreatedAt = null;
		dto.UpdatedAt = null;

		return Results.Ok(dto);
	}

	[HttpGet("{id}/content")]
	[ProducesResponseType(typeof(ContentResponseDto), 200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	[ProducesResponseType(501)]
	public async Task<IResult> GetContent(string id, CancellationToken cancellationToken, [FromQuery] string format = "markdown") {
		if (!Enum.TryParse<ExportFormat>(format, true, out var exportFormat)) {
			throw new ArgumentException($"Unsupported format: {format}");
		}

		if (exportFormat is not (ExportFormat.Markdown or ExportFormat.Html)) {
			throw new ArgumentException($"Content format must be 'markdown' or 'html', got: {format}");
		}

		var renderer = rendererFactory.Resolve(exportFormat) ?? throw new UnsupportedOperationException($"Renderer for format {format} is not implemented");

		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		var series = await service.FindOne(guid, cancellationToken)
			?? throw new EntityNotFoundException($"Series with id {id} not found");

		if (series.Metadata?.Description == null || series.Metadata.Description.Count == 0) {
			return Results.Ok(new ContentResponseDto {
				Data = string.Empty,
				Type = "text/markdown"
			});
		}

		var content = renderer.RenderDescription(series.Metadata.Description);
		var contentType = exportFormat == ExportFormat.Html ? "text/html" : "text/markdown";
		return Results.Ok(new ContentResponseDto {
			Data = content,
			Type = contentType
		});
	}

	[HttpGet]
	[ProducesResponseType(typeof(PagedResult<SeriesResponseDto>), 200)]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken) {
		var pagedSeries = await service.FindAll(pagination.ToApplicationDto(), cancellationToken);
		var items = pagedSeries.Items.Select(mapper.ToSeriesDto).ToList();
		var pagedDto = new PagedResult<SeriesResponseDto>(
			items,
			pagedSeries.TotalCount,
			pagedSeries.PageIndex,
			pagedSeries.PageSize
			);
		return Results.Ok(pagedDto);
	}

	[HttpPost]
	[ProducesResponseType(typeof(SeriesResponseDto), 200)]
	[ProducesResponseType(typeof(SeriesResponseDto), 201)]
	public async Task<IResult> Create([FromBody] CreateSeriesRequestDto dto, CancellationToken cancellationToken) {
		var (series, created) = await service.GetOrCreate(dto, cancellationToken);
		var responseDto = mapper.ToSeriesDto(series);
		return created
			? Results.Created($"{responseDto.Id}", responseDto)
			: Results.Ok(responseDto);
	}

	[HttpPost("{id}/sync")]
	[ProducesResponseType(200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	public async Task<IResult> SyncTree(string id, [FromBody] SyncSeriesRequestDto dto, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		await syncService.SyncSeriesTree(guid, dto, cancellationToken);
		return Results.Ok();
	}

	[HttpGet("{id}/tree")]
	[ProducesResponseType(typeof(BookTreeDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IResult> GetTree(string id, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		var tree = await bookTreeService.GetTree(guid, cancellationToken: cancellationToken);
		return Results.Ok(tree);
	}

	[HttpPatch("{id}")]
	[ProducesResponseType(typeof(SeriesResponseDto), 200)]
	public async Task<IResult> Update(string id, [FromBody] UpdateSeriesRequestDto dto, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		var updatedSeries = await service.Update(guid, dto, cancellationToken);
		return Results.Ok(mapper.ToSeriesDto(updatedSeries));
	}

	[HttpDelete("{id}")]
	[ProducesResponseType(typeof(bool), 200)]
	public async Task<IResult> Delete(string id, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		var result = await service.Delete(guid, cancellationToken);
		return Results.Ok(result);
	}

	[HttpGet("{id}/volumes")]
	[ProducesResponseType(typeof(IEnumerable<VolumeResponseDto>), 200)]
	public async Task<IResult> GetVolumes(string id, [FromQuery] PaginationRequestDto? pagination, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		if (pagination == null) {
			var series = await service.FindAllVolumes(guid, cancellationToken);
			var dto = series.Select(mapper.ToVolumeDto);
			return Results.Ok(dto);
		}

		var pagedVolumes = await service.FindAllVolumes(guid, pagination.ToApplicationDto(), cancellationToken);
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
