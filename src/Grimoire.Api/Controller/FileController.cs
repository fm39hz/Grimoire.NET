namespace Grimoire.Api.Controller;

using Application.Common;
using Application.Dto.Book;
using Application.Mapper;
using Application.Service.Contract;
using Constant;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{RouteConstant.CONTROLLER}")]
public class FileController(IStorageService storageService, IBookMapper mapper) : ControllerBase {
	[HttpPost("upload/{seriesId}")]
	[ProducesResponseType(typeof(AssetResponseDto), 200)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> Upload(string seriesId, IFormFile file, [FromQuery] string refType = "Content") {
		if (file.Length == 0) {
			return BadRequest("File is empty.");
		}

		var guid = PrefixedId.ToGuid(seriesId, EntityPrefix.Series);
		await using var stream = file.OpenReadStream();
		var asset = await storageService.UploadAssetAsync(guid, stream, file.ContentType, file.FileName,
			refType);
		return Ok(mapper.ToAssetDto(asset));
	}

	[HttpGet("{assetId}")]
	[ProducesResponseType(typeof(FileContentResult), 200)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> Get(string assetId) {
		var guid = PrefixedId.ToGuid(assetId, EntityPrefix.Asset);
		var stream = await storageService.GetFileStreamAsync(guid);
		if (stream == null) {
			return NotFound();
		}

		return File(stream, "application/octet-stream");
	}

	[HttpDelete("{assetId}")]
	[ProducesResponseType(204)]
	public async Task<IActionResult> Delete(string assetId) {
		var guid = PrefixedId.ToGuid(assetId, EntityPrefix.Asset);
		await storageService.DeleteFileAsync(guid);
		return NoContent();
	}
}
