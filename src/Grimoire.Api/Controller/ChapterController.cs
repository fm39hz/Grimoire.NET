namespace Grimoire.Api.Controller;

using Application.Common;
using Application.Dto.Book;
using Application.Mapper;
using Application.Service.Contract;
using Constant;
using Domain.Common;
using Dto;
using Microsoft.AspNetCore.Mvc;
using static Application.Common.SegmentMarkdownConverter;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class ChapterController(IChapterService service, IBookMapper mapper) : ControllerBase {
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(ChapterResponseDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IResult> FindOne(string id, [FromQuery] bool? timestamp = false,
		[FromQuery] bool? markdown = false) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Chapter);
		var chapter = await service.FindOne(guid);
		if (chapter is null) {
			return Results.NotFound();
		}

		var dto = mapper.ToChapterDto(chapter);
		if (timestamp != true) {
			dto.CreatedAt = null;
			dto.UpdatedAt = null;
		}

		if (markdown == true) {
			dto.Markdown = ConvertToMarkdown(dto.Content, dto.Footnotes);
		}

		return Results.Ok(dto);
	}

	[HttpGet]
	[ProducesResponseType(typeof(PagedResult<ChapterListResponseDto>), 200)]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto? pagination) {
		if (pagination == null) {
			var chapters = await service.FindAll();
			var dto = chapters.Select(mapper.ToChapterListDto);
			return Results.Ok(dto);
		}

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
}
