namespace Grimoire.Api.Controller;

using Application.Common;
using Constant;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{RouteConstant.CONTROLLER}")]
public class FileController(IStorageRepository storageRepository) : ControllerBase {
	[HttpPost("upload/{seriesId}")]
	[ProducesResponseType(typeof(AssetModel), 200)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> Upload(string seriesId, IFormFile file, [FromQuery] string refType = "Content") {
		if (file.Length == 0) {
			return BadRequest("File is empty.");
		}

		var guid = PrefixedId.ToGuid(seriesId);
		await using var stream = file.OpenReadStream();
		var asset = await storageRepository.UploadAssetAsync(guid, stream, file.ContentType, file.FileName,
			refType);
		return Ok(asset);
	}

	[HttpGet("{assetId}")]
	[ProducesResponseType(typeof(FileContentResult), 200)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> Get(string assetId) {
		var guid = PrefixedId.ToGuid(assetId);
		var fileBytes = await storageRepository.GetFileAsync(guid);
		if (fileBytes.Length == 0) {
			return NotFound();
		}

		return File(fileBytes, "application/octet-stream");
	}

	[HttpDelete("{assetId}")]
	[ProducesResponseType(204)]
	public async Task<IActionResult> Delete(string assetId) {
		var guid = PrefixedId.ToGuid(assetId);
		await storageRepository.DeleteFileAsync(guid);
		return NoContent();
	}
}
