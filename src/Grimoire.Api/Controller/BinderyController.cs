namespace Grimoire.Api.Controller;

using Application.Common;
using Application.Dto.Book;
using Application.Service.Contract;
using Constant;
using Microsoft.AspNetCore.Mvc;

/// <summary>
///     Controller for exporting series with custom structure and layout
/// </summary>
[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class BinderyController(IBinderyService binderyService) : ControllerBase {
	/// <summary>
	///     Export a series with custom structure and layout
	/// </summary>
	/// <param name="seriesId">The series ID to export</param>
	/// <param name="request">Export configuration including format, mode, target volumes, and structure</param>
	/// <returns>Export file</returns>
	[HttpPost]
	[ProducesResponseType(typeof(FileResult), 200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	public async Task<IResult> ExportSeries([FromQuery] string seriesId, [FromBody] BinderyRequestDto request) {
		var guid = PrefixedId.ToGuid(seriesId, EntityPrefix.Series);
		var result = await binderyService.ExportSeriesAsync(guid, request);

		if (!result.Success) {
			return Results.BadRequest(new { error = result.ErrorMessage });
		}

		return Results.File(result.ContentStream, result.ContentType, result.FileName);
	}
}
