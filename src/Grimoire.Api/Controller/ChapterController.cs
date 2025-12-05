namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Application.Dto.Common;
using Application.Mapper;
using Application.Service.Contract;
using Constant;
using Dto;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class ChapterController(IChapterService service, IBookMapper mapper) : ControllerBase {
	[HttpGet("{id:guid}")]
	[ProducesResponseType(typeof(ChapterResponseDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IResult> FindOne(Guid id) {
		var chapter = await service.FindOne(id);
		return chapter is null? Results.NotFound() : Results.Ok(mapper.ToChapterDto(chapter));
	}

	[HttpGet]
	[ProducesResponseType(typeof(PagedResult<ChapterResponseDto>), 200)]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto? pagination) {
		if (pagination == null) {
			var chapters = await service.FindAll();
			var dto = chapters.Select(mapper.ToChapterDto);
			return Results.Ok(dto);
		}

		var pagedChapters = await service.FindAll(pagination.ToApplicationDto());
		var pagedDto = new PagedResult<ChapterResponseDto>(
			pagedChapters.Items.Select(mapper.ToChapterDto).ToList(),
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
		return Results.Created($"{createdChapter.Id}", mapper.ToChapterDto(createdChapter));
	}

	[HttpPut("{id:guid}")]
	[ProducesResponseType(typeof(ChapterResponseDto), 200)]
	public async Task<IResult> Update(Guid id, [FromBody] UpdateChapterRequestDto dto) {
		var updatedChapter = await service.Update(id, dto);
		return Results.Ok(mapper.ToChapterDto(updatedChapter));
	}

	[HttpDelete("{id:guid}")]
	[ProducesResponseType(typeof(bool), 200)]
	public async Task<IResult> Delete(Guid id) {
		var result = await service.Delete(id);
		return Results.Ok(result);
	}
}
