namespace Grimoire.Api.Controller;

using System.Net.Mime;
using System.Security.Cryptography;
using Domain.Common.Repository;
using Domain.Constant;
using Domain.Entity.Book;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public class FileController(IStorageRepository storageRepository, IAssetRepository assetRepository) : ControllerBase {
	[HttpPost("upload")]
	public async Task<IActionResult> Upload(IFormFile file, [FromQuery] Guid seriesId) {
		if (file.Length == 0) {
			return BadRequest("File is empty.");
		}

		await using var stream = file.OpenReadStream();
		var fileHash = await ComputeHashAsync(stream);

		var existingAsset = await assetRepository.GetByFileHashAsync(fileHash);
		if (existingAsset is not null) {
			return Ok(new { AssetKey = existingAsset.Id });
		}

		var assetId = Guid.NewGuid();
		var assetPath = $"{seriesId}/assets/{assetId}{Path.GetExtension(file.FileName)}";

		stream.Seek(0, SeekOrigin.Begin);
		var savedFilePath = await storageRepository.SaveFileAsync(assetPath, stream, file.ContentType);

		var asset = new AssetModel {
			Id = assetId,
			SeriesId = seriesId,
			Path = savedFilePath,
			FileHash = fileHash,
			RefType = "Content" // Or determine based on context
		};

		await assetRepository.Create(asset);

		return Ok(new { AssetKey = asset.Id });
	}

	private static async Task<string> ComputeHashAsync(Stream stream) {
		using var sha256 = SHA256.Create();
		var hashBytes = await sha256.ComputeHashAsync(stream);
		return Convert.ToHexStringLower(hashBytes);
	}

	[HttpGet("download/{assetId:guid}")]
	public async Task<IActionResult> Download(Guid assetId) {
		var asset = await assetRepository.FindOne(assetId);
		if (asset is null) {
			return NotFound();
		}

		var fileBytes = await storageRepository.GetFileAsync(asset.Path);
		if (fileBytes.Length == 0) {
			return NotFound();
		}

		const string contentType = "application/octet-stream";
		// In a real application, you would determine the content type based on the file extension or metadata
		// For now, we'll use a generic one.

		return File(fileBytes, contentType,
			new ContentDisposition {
				FileName = Path.GetFileName(asset.Path), DispositionType = DispositionTypeNames.Attachment
			}.ToString());
	}

	[HttpDelete("{assetId:guid}")]
	public async Task<IActionResult> Delete(Guid assetId) {
		var asset = await assetRepository.FindOne(assetId);
		if (asset is null) {
			return NotFound();
		}

		await storageRepository.DeleteFileAsync(asset.Path);
		await assetRepository.Delete(asset.Id);

		return NoContent();
	}
}
