namespace Grimoire.Api.Controller;

using Constant;
using Domain.Common.Repository;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{RouteConstant.CONTROLLER}")]
public class FileController(IStorageRepository storageRepository) : ControllerBase {
	[HttpPost("upload/{seriesId:guid}")]
	public async Task<IActionResult> Upload(Guid seriesId, IFormFile file, [FromQuery] string refType = "Content") {
		if (file.Length == 0) {
			return BadRequest("File is empty.");
		}

		await using var stream = file.OpenReadStream();
		var asset = await storageRepository.UploadAssetAsync(seriesId, stream, file.ContentType, file.FileName,
			refType);
		return Ok(asset);
	}

	[HttpGet("{assetId:guid}")]
	public async Task<IActionResult> Get(Guid assetId) {
		var fileBytes = await storageRepository.GetFileAsync(assetId);
		if (fileBytes.Length == 0) {
			return NotFound();
		}

		return File(fileBytes, "application/octet-stream");
	}

	[HttpDelete("{assetId:guid}")]
	public async Task<IActionResult> Delete(Guid assetId) {
		await storageRepository.DeleteFileAsync(assetId);
		return NoContent();
	}
}
