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
public sealed class VolumeController(IVolumeService service, IBookMapper mapper) : ControllerBase {
	[HttpGet("{id:guid}")]
	[ProducesResponseType(typeof(VolumeResponseDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IResult> FindOne(Guid id) {
		var volume = await service.FindOne(id);
		return volume is null? Results.NotFound() : Results.Ok(mapper.ToVolumeDto(volume));
	}

	[HttpGet]
	[ProducesResponseType(typeof(PagedResult<VolumeResponseDto>), 200)]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto? pagination) {
		if (pagination == null) {
			var volumes = await service.FindAll();
			var dto = volumes.Select(mapper.ToVolumeDto);
			return Results.Ok(dto);
		}

		var pagedVolumes = await service.FindAll(pagination.ToApplicationDto());
		var pagedDto = new PagedResult<VolumeResponseDto>(
			pagedVolumes.Items.Select(mapper.ToVolumeDto).ToList(),
			pagedVolumes.TotalCount,
			pagedVolumes.PageIndex,
			pagedVolumes.PageSize
			);
		return Results.Ok(pagedDto);
	}

	[HttpPost]
	[ProducesResponseType(typeof(VolumeResponseDto), 201)]
	public async Task<IResult> Create([FromBody] CreateVolumeRequestDto dto) {
		var createdVolume = await service.Create(dto);
		return Results.Created($"{createdVolume.Id}", mapper.ToVolumeDto(createdVolume));
	}

	[HttpPut("{id:guid}")]
	[ProducesResponseType(typeof(VolumeResponseDto), 200)]
	public async Task<IResult> Update(Guid id, [FromBody] UpdateVolumeRequestDto dto) {
		var updatedVolume = await service.Update(id, dto);
		return Results.Ok(mapper.ToVolumeDto(updatedVolume));
	}

	[HttpDelete("{id:guid}")]
	[ProducesResponseType(typeof(bool), 200)]
	public async Task<IResult> Delete(Guid id) {
		var result = await service.Delete(id);
		return Results.Ok(result);
	}

	[HttpGet("{id:guid}/chapters")]
	[ProducesResponseType(typeof(IEnumerable<ChapterListResponseDto>), 200)]
	public async Task<IResult> GetChapters(Guid id, [FromQuery] PaginationRequestDto? pagination) {
		if (pagination == null) {
			var chapters = await service.FindAllChapters(id);
			var dto = chapters.Select(mapper.ToChapterListDto);
			return Results.Ok(dto);
		}

		var pagedChapters = await service.FindAllChapters(id, pagination.ToApplicationDto());
		var pagedDto = new PagedResult<ChapterListResponseDto>(
			pagedChapters.Items.Select(mapper.ToChapterListDto).ToList(),
			pagedChapters.TotalCount,
			pagedChapters.PageIndex,
			pagedChapters.PageSize
			);
		return Results.Ok(pagedDto);
	}
}
