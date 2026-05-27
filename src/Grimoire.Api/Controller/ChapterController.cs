namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Application.Mapper;
using Application.Service.Contract;
using Application.Service.Strategy;
using Constant;
using Domain.Common;
using Domain.Entity.Book.Segment;
using Domain.Exception;
using Dto;
using Infrastructure.Export.Common;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class ChapterController(
	IChapterService service,
	IBookMapper mapper,
	ISectionRendererFactory rendererFactory,
	IAssetService assetService) : ControllerBase {
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(ChapterResponseDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IResult> FindOne(string id, [FromQuery] bool? timestamp = false) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Chapter);
		var chapter = await service.FindOne(guid);
		if (chapter is null) {
			return Results.NotFound();
		}

		var dto = mapper.ToChapterDto(chapter);
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
	public async Task<IResult> GetContent(string id, [FromQuery] string format = "markdown") {
		if (!Enum.TryParse<ExportFormat>(format, true, out var exportFormat)) {
			throw new ArgumentException($"Unsupported format: {format}");
		}

		if (exportFormat is not (ExportFormat.Markdown or ExportFormat.Html)) {
			throw new ArgumentException($"Content format must be 'markdown' or 'html', got: {format}");
		}

		var renderer = rendererFactory.Resolve(exportFormat) ?? throw new UnsupportedOperationException($"Renderer for format {format} is not implemented");

		var guid = PrefixedId.ToGuid(id, EntityPrefix.Chapter);
		var chapter = await service.FindOne(guid)
			?? throw new EntityNotFoundException($"Chapter with id {id} not found");

		if (chapter.ContentData == null) {
			return Results.Ok(new ContentResponseDto {
				Data = string.Empty,
				Type = "text/markdown"
			});
		}

		var assets = await ResolveContentAssets(chapter.ContentData.Segments);

		var content = renderer.RenderSegments(chapter.ContentData.Segments, chapter.ContentData.Footnotes);
		var contentType = exportFormat == ExportFormat.Html ? "text/html" : "text/markdown";
		return Results.Ok(new ContentResponseDto {
			Data = content,
			Type = contentType,
			Assets = assets
		});
	}

	[HttpGet]
	[ProducesResponseType(typeof(PagedResult<ChapterListResponseDto>), 200)]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto pagination) {
		var pagedChapters = await service.FindAll(pagination.ToApplicationDto());
		var pagedDto = new PagedResult<ChapterListResponseDto>(
			pagedChapters.Items.Select(mapper.ToChapterListDto).ToList(),
			pagedChapters.TotalCount,
			pagedChapters.PageIndex,
			pagedChapters.PageSize
			);
		return Results.Ok(pagedDto);
	}

	[HttpPost]
	[ProducesResponseType(typeof(ChapterResponseDto), 201)]
	public async Task<IResult> Create([FromBody] CreateChapterRequestDto dto) {
		var createdChapter = await service.CreateFromImportAsync(dto);
		var responseDto = mapper.ToChapterDto(createdChapter);
		return Results.Created($"{responseDto.Id}", responseDto);
	}

	[HttpPatch("{id}")]
	[ProducesResponseType(typeof(ChapterResponseDto), 200)]
	public async Task<IResult> Update(string id, [FromBody] UpdateChapterRequestDto dto) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Chapter);
		var updatedChapter = await service.Update(guid, dto);
		return Results.Ok(mapper.ToChapterDto(updatedChapter));
	}

	[HttpDelete("{id}")]
	[ProducesResponseType(typeof(bool), 200)]
	public async Task<IResult> Delete(string id) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Chapter);
		var result = await service.Delete(guid);
		return Results.Ok(result);
	}

	[HttpPost("{id}/split")]
	[ProducesResponseType(typeof(IEnumerable<ChapterResponseDto>), 201)]
	[ProducesResponseType(404)]
	[ProducesResponseType(400)]
	public async Task<IResult> Split(string id, [FromBody] SplitChapterRequestDto dto) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Chapter);
		var resultChapters = await service.SplitAsync(guid, dto);
		var responseDtos = resultChapters.Select(mapper.ToChapterDto);
		return Results.Created("/api/v1/chapter", responseDtos);
	}

	private async Task<IReadOnlyList<AssetListingDto>> ResolveContentAssets(
		IReadOnlyList<Domain.Entity.Book.SegmentModel> segments) {
		var assetIds = segments
			.OfType<ImageSegmentModel>()
			.Select(s => PrefixedId.TryToGuid(s.AssetKey, EntityPrefix.Asset, out var id) ? id : Guid.Empty)
			.Where(id => id != Guid.Empty)
			.Distinct()
			.ToList();

		if (assetIds.Count == 0) {
			return [];
		}

		var assets = await assetService.FindByIdsAsync(assetIds);

		return assets.Values.Select(a => new AssetListingDto {
			Id = PrefixedId.ToString(EntityPrefix.Asset, a.Id),
			RefType = a.RefType.ToString(),
			FileName = a.OriginalFileName
		}).ToList();
	}
}
