namespace Grimoire.Api.Controller;

using System.Threading;
using Application.Dto.Book;
using Application.Dto.Book.Restructure;
using Application.Dto.Book.Tree;
using Application.Export;
using Application.Mapper;
using Application.Ingestion.Legacy;
using Application.Ingestion.Execution;
using Application.Ingestion.Configuration;
using Microsoft.Extensions.Options;
using Application.Service.Contract;
using Application.Service.Strategy;
using Constant;
using Domain.Common;
using Domain.Exception;
using Dto;
using Extension;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class SeriesController(
	ISeriesService service,
	ISeriesSyncService syncService,
	IBookTreeService bookTreeService,
	IBookRestructureService restructureService,
	IBookMapper mapper,
	ILegacySourcePackageAdapter legacyAdapter,
	ILegacyShadowAnalyzer shadowAnalyzer,
	IImportExecutionService importExecutionService,
	IOptions<IngestionCoreOptions> ingestionOptions,
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

		var dto = mapper.ToSeriesDto(series).ApplyTimestampOption(timestamp);
		return Results.Ok(dto);
	}

	[HttpGet("{id}/content")]
	[ProducesResponseType(typeof(ContentResponseDto), 200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	[ProducesResponseType(501)]
	public async Task<IResult> GetContent(string id, CancellationToken cancellationToken, [FromQuery] string format = "markdown") {
		var renderer = rendererFactory.ResolveAndValidate(format, out var exportFormat);

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
		var package = legacyAdapter.FromSeriesSync(guid, dto);
		var shadowRunId = await shadowAnalyzer.Analyze(package, cancellationToken);
		if (ingestionOptions.Value.Enabled && ingestionOptions.Value.LegacyAdaptersEnabled) {
			var analyzed = await HttpContext.RequestServices
				.GetRequiredService<Application.Ingestion.Analysis.IImportAnalysisService>()
				.Analyze(package, cancellationToken);
			var committed = await importExecutionService.Commit(Guid.Parse(analyzed.Id[4..]), safeOnly: true, cancellationToken);
			return committed.Status == nameof(Domain.Entity.Ingestion.ImportRunStatus.Committed)
				? Results.Ok(committed)
				: Results.Conflict(committed);
		}
		await syncService.SyncSeriesTree(guid, dto, cancellationToken);
		await shadowAnalyzer.RecordOutcome(shadowRunId, new { SeriesId = guid, VolumesReceived = dto.Volumes.Count }, cancellationToken);
		return Results.Ok();
	}

	[HttpPost("{id}/restructure")]
	[ProducesResponseType(typeof(BookTreeDto), 200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	public async Task<IResult> Restructure(string id, [FromBody] BookRestructureRequestDto dto, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		var tree = await restructureService.ExecuteAsync(dto, cancellationToken);
		return Results.Ok(tree);
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
	[ProducesResponseType(204)]
	public async Task<IResult> Delete(string id, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Series);
		_ = await service.Delete(guid, cancellationToken);
		return Results.NoContent();
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


}
