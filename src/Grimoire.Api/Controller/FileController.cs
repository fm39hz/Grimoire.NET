namespace Grimoire.Api.Controller;

using Application.Common;
using Application.Dto.Book;
using Application.Mapper;
using Constant;
using Domain.Common.Repository;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{RouteConstant.CONTROLLER}")]
public class FileController(IStorageRepository storageRepository, IBookMapper mapper) : ControllerBase {
	[HttpPost("upload/{seriesId}")]
	[ProducesResponseType(typeof(AssetResponseDto), 200)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> Upload(string seriesId, IFormFile file, [FromQuery] string refType = "Content") {
		if (file.Length == 0) {
			return BadRequest("File is empty.");
		}

		var guid = PrefixedId.ToGuid(seriesId, EntityPrefix.Series);
		await using var stream = file.OpenReadStream();
		var asset = await storageRepository.UploadAssetAsync(guid, stream, file.ContentType, file.FileName,
			refType);
		return Ok(mapper.ToAssetDto(asset));
	}

	[HttpGet("{assetId}")]
	[ProducesResponseType(typeof(FileContentResult), 200)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> Get(string assetId) {
		var guid = PrefixedId.ToGuid(assetId, EntityPrefix.Asset);
		var fileBytes = await storageRepository.GetFileAsync(guid);
		if (fileBytes.Length == 0) {
			return NotFound();
		}

		return File(fileBytes, "application/octet-stream");
	}

	[HttpDelete("{assetId}")]
	[ProducesResponseType(204)]
	public async Task<IActionResult> Delete(string assetId) {
		var guid = PrefixedId.ToGuid(assetId, EntityPrefix.Asset);
		await storageRepository.DeleteFileAsync(guid);
		return NoContent();
	}
}
