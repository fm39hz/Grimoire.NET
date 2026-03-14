namespace Grimoire.Api.Controller;

using Application.Common;
using Application.Dto.Book;
using Application.Mapper;
using Application.Service.Contract;
using Constant;
using Domain.Common;
using Dto;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class VolumeController(IVolumeService service, IBookMapper mapper) : ControllerBase {
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(VolumeResponseDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IResult> FindOne(string id) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Volume);
		var volume = await service.FindOne(guid);
		return volume is null ? Results.NotFound() : Results.Ok(mapper.ToVolumeDto(volume));
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
		var responseDto = mapper.ToVolumeDto(createdVolume);
		return Results.Created($"{responseDto.Id}", responseDto);
	}

	[HttpPatch("{id}")]
	[ProducesResponseType(typeof(VolumeResponseDto), 200)]
	public async Task<IResult> Update(string id, [FromBody] UpdateVolumeRequestDto dto) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Volume);
		var updatedVolume = await service.Update(guid, dto);
		return Results.Ok(mapper.ToVolumeDto(updatedVolume));
	}

	[HttpDelete("{id}")]
	[ProducesResponseType(typeof(bool), 200)]
	public async Task<IResult> Delete(string id) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Volume);
		var result = await service.Delete(guid);
		return Results.Ok(result);
	}

	[HttpGet("{id}/chapters")]
	[ProducesResponseType(typeof(IEnumerable<ChapterListResponseDto>), 200)]
	public async Task<IResult> GetChapters(string id, [FromQuery] PaginationRequestDto? pagination) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Volume);
		if (pagination == null) {
			var chapters = await service.FindAllChapters(guid);
			var dto = chapters.Select(mapper.ToChapterListDto);
			return Results.Ok(dto);
		}

		var pagedChapters = await service.FindAllChapters(guid, pagination.ToApplicationDto());
		var pagedDto = new PagedResult<ChapterListResponseDto>(
			pagedChapters.Items.Select(mapper.ToChapterListDto).ToList(),
			pagedChapters.TotalCount,
			pagedChapters.PageIndex,
			pagedChapters.PageSize
			);
		return Results.Ok(pagedDto);
	}
}
