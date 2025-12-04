namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Application.Mapper;
using Application.Service.Contract;
using Constant;
using Dto;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class ChapterController(IChapterService service, IBookMapper mapper) : ControllerBase {
	[HttpGet("{id:guid}")]
	public async Task<IResult> FindOne(Guid id) {
		var chapter = await service.FindOne(id);
		return chapter is null? Results.NotFound() : Results.Ok(mapper.ToChapterDto(chapter));
	}

	[HttpGet]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto? pagination) {
		var chapters = await service.FindAll();
		var dto = chapters.Select(mapper.ToChapterDto);
		return Results.Ok(dto);
	}

	[HttpPost]
	public async Task<IResult> Create([FromBody] CreateChapterRequestDto dto) {
		var createdChapter = await service.CreateFromImportAsync(dto);
		return Results.Created($"{createdChapter.Id}", mapper.ToChapterDto(createdChapter));
	}

	[HttpPut("{id:guid}")]
	public async Task<IResult> Update(Guid id, [FromBody] UpdateChapterRequestDto dto) {
		var updatedChapter = await service.Update(id, dto);
		return Results.Ok(mapper.ToChapterDto(updatedChapter));
	}

	[HttpDelete("{id:guid}")]
	public async Task<IResult> Delete(Guid id) {
		var result = await service.Delete(id);
		return Results.Ok(result);
	}
}
