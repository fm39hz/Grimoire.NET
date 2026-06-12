namespace Grimoire.Api.Controller;

using Application.Dto.Book;
using Application.Mapper;
using Application.Service.Contract;
using Constant;
using Domain.Common;
using Domain.Entity.Book;
using System.Threading;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{RouteConstant.CONTROLLER}")]
public class FileController(IStorageService storageService, IBookMapper mapper) : ControllerBase {
	[HttpPost("series/{seriesId}")]
	[ProducesResponseType(typeof(AssetResponseDto), 200)]
	[ProducesResponseType(400)]
	public async Task<IResult> Upload(string seriesId, IFormFile file, CancellationToken cancellationToken, [FromQuery] string refType = "Content") {
		if (file.Length == 0) {
			return Results.BadRequest("File is empty.");
		}

		if (!Enum.TryParse<AssetRefType>(refType, true, out var assetRefType)) {
			return Results.BadRequest("Invalid refType. Must be 'Cover' or 'Content'.");
		}

		var guid = PrefixedId.ToGuid(seriesId, EntityPrefix.Series);
		await using var stream = file.OpenReadStream();
		var asset = await storageService.UploadAssetAsync(guid, stream, file.ContentType, file.FileName,
			assetRefType, cancellationToken: cancellationToken);
		return Results.Ok(mapper.ToAssetDto(asset));
	}

	[HttpGet("{assetId}")]
	[ProducesResponseType(typeof(FileContentResult), 200)]
	[ProducesResponseType(404)]
	public async Task<IResult> Get(string assetId, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(assetId, EntityPrefix.Asset);
		var result = await storageService.GetFileStreamAsync(guid, cancellationToken);
		return result == null ? Results.NotFound() : Results.Stream(result.Stream, result.ContentType, result.FileName);
	}

	[HttpDelete("{assetId}")]
	[ProducesResponseType(204)]
	public async Task<IResult> Delete(string assetId, CancellationToken cancellationToken) {
		var guid = PrefixedId.ToGuid(assetId, EntityPrefix.Asset);
		await storageService.DeleteFileAsync(guid, cancellationToken);
		return Results.NoContent();
	}
}
