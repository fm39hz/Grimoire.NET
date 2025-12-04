namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Application.Mapper;
using Application.Service.Contract;
using Constant;
using Dto;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class VolumeController(IVolumeService service, IBookMapper mapper) : ControllerBase {
	[HttpGet("{id:guid}")]
	public async Task<IResult> FindOne(Guid id) {
		var volume = await service.FindOne(id);
		return volume is null? Results.NotFound() : Results.Ok(mapper.ToVolumeDto(volume));
	}

	[HttpGet]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto? pagination) {
		var volumes = await service.FindAll();
		var dto = volumes.Select(mapper.ToVolumeDto);
		return Results.Ok(dto);
	}

	[HttpPost]
	public async Task<IResult> Create([FromBody] CreateVolumeRequestDto dto) {
		var createdVolume = await service.Create(dto);
		return Results.Created($"{createdVolume.Id}", mapper.ToVolumeDto(createdVolume));
	}

	[HttpPut("{id:guid}")]
	public async Task<IResult> Update(Guid id, [FromBody] UpdateVolumeRequestDto dto) {
		var updatedVolume = await service.Update(id, dto);
		return Results.Ok(mapper.ToVolumeDto(updatedVolume));
	}

	[HttpDelete("{id:guid}")]
	public async Task<IResult> Delete(Guid id) {
		var result = await service.Delete(id);
		return Results.Ok(result);
	}
}
