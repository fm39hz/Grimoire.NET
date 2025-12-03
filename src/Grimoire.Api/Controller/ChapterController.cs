namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Application.Dto.Common;
using Application.Service.Contract;
using Domain.Constant;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class ChapterController(IChapterService service) : ControllerBase {
    [HttpGet("{id:guid}")]
    public async Task<IResult> FindOne(Guid id) {
        var chapter = await service.FindOne(id);
        return chapter is null ? Results.NotFound() : Results.Ok(new ChapterResponseDto(chapter));
    }

    [HttpGet]
    public async Task<IResult> FindAll([FromQuery] PaginationRequestDto? pagination) {
        var chapters = await service.FindAll();
        var dto = chapters.Select(s => new ChapterResponseDto(s));
        return Results.Ok(dto);
    }

    [HttpPost]
    public async Task<IResult> Create([FromBody] ChapterRequestDto entity) {
		var createdChapter = await service.CreateFromImportAsync(entity);
        return Results.Created($"/api/v1/chapter/{createdChapter.Id}", new ChapterResponseDto(createdChapter));
    }

    [HttpPut("{id:guid}")]
    public async Task<IResult> Update(Guid id, [FromBody] ChapterRequestDto entity) {
        var updatedChapter = await service.Update(id, entity.ToModel());
        return Results.Ok(new ChapterResponseDto(updatedChapter));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(Guid id) {
        var result = await service.Delete(id);
        return Results.Ok(result);
    }
}
