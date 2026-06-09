namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Application.Export;
using Application.Mapper;
using Application.Service.Contract;
using Application.Service.Strategy;
using Constant;
using Domain.Common;
using Domain.Entity.Book.Segment;
using Domain.Exception;
using Dto;
using Infrastructure.Export.Common;
using Infrastructure.Export.Epub;
using Infrastructure.Export.Markdown;
using System.Threading;
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
	public async Task<IResult> FindOne(string id, CancellationToken cancellationToken, [FromQuery] bool? timestamp = false) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Chapter);
		var chapter = await service.FindOne(guid, cancellationToken);
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
	public async Task<IResult> GetContent(
		string id,
		CancellationToken cancellationToken,
		[FromQuery] string format = "markdown",
		[FromQuery] FootnoteStyle footnoteStyle = FootnoteStyle.Parentheses,
		[FromQuery] bool enableDropcap = false) {
		if (!Enum.TryParse<ExportFormat>(format, true, out var exportFormat)) {
			throw new ArgumentException($"Unsupported format: {format}");
		}

		if (exportFormat is not (ExportFormat.Markdown or ExportFormat.Html)) {
			throw new ArgumentException($"Content format must be 'markdown' or 'html', got: {format}");
		}

		var renderer = rendererFactory.Resolve(exportFormat) ?? throw new UnsupportedOperationException($"Renderer for format {format} is not implemented");

		var guid = PrefixedId.ToGuid(id, EntityPrefix.Chapter);
		var chapter = await service.FindOne(guid, cancellationToken)
			?? throw new EntityNotFoundException($"Chapter with id {id} not found");

		if (chapter.ContentData == null) {
			return Results.Ok(new ContentResponseDto {
				Data = string.Empty,
				Type = "text/markdown"
			});
		}

		var assets = await ResolveContentAssets(chapter.ContentData.Segments, cancellationToken);

		string content;
		if (renderer is EpubSectionRenderer epubRenderer) {
			content = epubRenderer.RenderSegments(chapter.ContentData.Segments, chapter.ContentData.Footnotes, null, footnoteStyle, enableDropcap);
		} else if (renderer is MarkdownSectionRenderer mdRenderer) {
			content = mdRenderer.RenderSegments(chapter.ContentData.Segments, chapter.ContentData.Footnotes, null, footnoteStyle, enableDropcap);
		} else {
			content = renderer.RenderSegments(chapter.ContentData.Segments, chapter.ContentData.Footnotes);
		}

		var contentType = exportFormat == ExportFormat.Html ? "text/html" : "text/markdown";
		return Results.Ok(new ContentResponseDto {
			Data = content,
			Type = contentType,
			Assets = assets
		});
	}

	[HttpGet]
	[ProducesResponseType(typeof(PagedResult<ChapterListResponseDto>), 200)]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken) {
		var pagedChapters = await service.FindAll(pagination.ToApplicationDto(), cancellationToken);
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
	public async Task<IResult> Create([FromBody] CreateChapterRequestDto dto, CancellationToken cancellationToken) {
		var createdChapter = await service.CreateFromImportAsync(dto, cancellationToken);
		var responseDto = mapper.ToChapterDto(createdChapter);
		return Results.Created($"{responseDto.Id}", responseDto);
	}

	[HttpPatch("{id}")]
	[ProducesResponseType(typeof(ChapterResponseDto), 200)]
	public async Task<IResult> Update(string id, [FromBody] UpdateChapterRequestDto dto, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Chapter);
		var updatedChapter = await service.Update(guid, dto, cancellationToken);
		return Results.Ok(mapper.ToChapterDto(updatedChapter));
	}

	[HttpDelete("{id}")]
	[ProducesResponseType(typeof(bool), 200)]
	public async Task<IResult> Delete(string id, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Chapter);
		var result = await service.Delete(guid, cancellationToken);
		return Results.Ok(result);
	}

	[HttpPost("merge")]
	[ProducesResponseType(typeof(ChapterResponseDto), 200)]
	[ProducesResponseType(404)]
	[ProducesResponseType(400)]
	public async Task<IResult> Merge([FromBody] MergeChaptersRequestDto dto, CancellationToken cancellationToken) {
		var mergedChapter = await service.MergeAsync(dto, cancellationToken);
		return Results.Ok(mapper.ToChapterDto(mergedChapter));
	}

	[HttpPost("{id}/split")]
	[ProducesResponseType(typeof(IEnumerable<ChapterResponseDto>), 201)]
	[ProducesResponseType(404)]
	[ProducesResponseType(400)]
	public async Task<IResult> Split(string id, [FromBody] SplitChapterRequestDto dto, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Chapter);
		var resultChapters = await service.SplitAsync(guid, dto, cancellationToken);
		var responseDtos = resultChapters.Select(mapper.ToChapterDto);
		return Results.Created("/api/v1/chapter", responseDtos);
	}

	private async Task<IReadOnlyList<AssetListingDto>> ResolveContentAssets(
		IReadOnlyList<Domain.Entity.Book.SegmentModel> segments,
		CancellationToken cancellationToken) {
		var assetIds = segments
			.OfType<ImageSegmentModel>()
			.Select(s => PrefixedId.TryToGuid(s.AssetKey, EntityPrefix.Asset, out var id) ? id : Guid.Empty)
			.Where(id => id != Guid.Empty)
			.Distinct()
			.ToList();

		if (assetIds.Count == 0) {
			return [];
		}

		var assets = await assetService.FindByIdsAsync(assetIds, cancellationToken);

		return assets.Values.Select(a => new AssetListingDto {
			Id = PrefixedId.ToString(EntityPrefix.Asset, a.Id),
			RefType = a.RefType.ToString(),
			FileName = Path.GetFileName(a.OriginalFileName)
		}).ToList();
	}
}
