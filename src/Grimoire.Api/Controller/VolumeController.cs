namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Grimoire.Api.Dto;
using Application.Service.Contract;
using Grimoire.Api.Constant;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class VolumeController(IVolumeService service) : ControllerBase {
	[HttpGet("{id:guid}")]
	public async Task<IResult> FindOne(Guid id) {
		var volume = await service.FindOne(id);
		return volume is null? Results.NotFound() : Results.Ok(new VolumeResponseDto(volume));
	}

	[HttpGet]
	public async Task<IResult> FindAll([FromQuery] PaginationRequestDto? pagination) {
		var volumes = await service.FindAll();
		var dto = volumes.Select(s => new VolumeResponseDto(s));
		return Results.Ok(dto);
	}

	[HttpPost]
	public async Task<IResult> Create([FromBody] CreateVolumeRequestDto dto) {
		var createdVolume = await service.Create(dto);
		return Results.Created($"/api/v1/volume/{createdVolume.Id}", new VolumeResponseDto(createdVolume));
	}

	[HttpPut("{id:guid}")]
	public async Task<IResult> Update(Guid id, [FromBody] UpdateVolumeRequestDto dto) {
		var updatedVolume = await service.Update(id, dto);
		return Results.Ok(new VolumeResponseDto(updatedVolume));
	}

	[HttpDelete("{id:guid}")]
	public async Task<IResult> Delete(Guid id) {
		var result = await service.Delete(id);
		return Results.Ok(result);
	}
}
