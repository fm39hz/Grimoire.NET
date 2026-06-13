namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Application.Mapper;
using Application.Service.Contract;
using Constant;
using Domain.Common;
using Dto;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Extension;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class VolumeController(IVolumeService service, IBookMapper mapper) : ControllerBase {
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(VolumeResponseDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IResult> FindOne(string id, CancellationToken cancellationToken, [FromQuery] bool? timestamp = false) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Volume);
		var volume = await service.FindOne(guid, cancellationToken);
		if (volume is null) {
			return Results.NotFound();
		}

		var dto = mapper.ToVolumeDto(volume).ApplyTimestampOption(timestamp);
		return Results.Ok(dto);
	}

	[HttpGet]
	[ProducesResponseType(typeof(PagedResult<VolumeResponseDto>), 200)]
	public async Task<IResult> FindAll(
		[FromQuery] PaginationRequestDto pagination,
		[FromServices] Grimoire.Application.Persistence.IVolumeProjectedQuery query,
		CancellationToken cancellationToken) {
		var pagedDto = await query.FindAllProjectedAsync(pagination.PageIndex, pagination.PageSize, cancellationToken);
		return Results.Ok(pagedDto);
	}

	[HttpPost]
	[ProducesResponseType(typeof(VolumeResponseDto), 201)]
	public async Task<IResult> Create([FromBody] CreateVolumeRequestDto dto, CancellationToken cancellationToken) {
		var createdVolume = await service.Create(dto, cancellationToken);
		var responseDto = mapper.ToVolumeDto(createdVolume);
		return Results.Created($"{responseDto.Id}", responseDto);
	}

	[HttpPatch("{id}")]
	[ProducesResponseType(typeof(VolumeResponseDto), 200)]
	public async Task<IResult> Update(string id, [FromBody] UpdateVolumeRequestDto dto, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Volume);
		var updatedVolume = await service.Update(guid, dto, cancellationToken);
		return Results.Ok(mapper.ToVolumeDto(updatedVolume));
	}

	[HttpDelete("{id}")]
	[ProducesResponseType(204)]
	public async Task<IResult> Delete(string id, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Volume);
		_ = await service.Delete(guid, cancellationToken);
		return Results.NoContent();
	}

	[HttpGet("{id}/chapters")]
	[ProducesResponseType(typeof(IEnumerable<ChapterListResponseDto>), 200)]
	public async Task<IResult> GetChapters(string id, [FromQuery] PaginationRequestDto? pagination, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(id, EntityPrefix.Volume);
		if (pagination == null) {
			var chapters = await service.FindAllChapters(guid, cancellationToken);
			var dto = chapters.Select(mapper.ToChapterListDto);
			return Results.Ok(dto);
		}

		var pagedChapters = await service.FindAllChapters(guid, pagination.ToApplicationDto(), cancellationToken);
		var pagedDto = new PagedResult<ChapterListResponseDto>(
			pagedChapters.Items.Select(mapper.ToChapterListDto).ToList(),
			pagedChapters.TotalCount,
			pagedChapters.PageIndex,
			pagedChapters.PageSize
			);
		return Results.Ok(pagedDto);
	}
}
