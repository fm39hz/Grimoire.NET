namespace Grimoire.Api.Controller;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Dto.Book;
using Application.Mapper;
using Constant;
using Domain.Common;
using Domain.Common.Repository;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class IngestionAuditsController(
	IIngestionAuditRepository auditRepository,
	IBookMapper mapper) : ControllerBase {

	[HttpGet("series/{seriesId}")]
	[ProducesResponseType(typeof(IEnumerable<IngestionAuditResponseDto>), 200)]
	[ProducesResponseType(400)]
	public async Task<IResult> GetHistory(string seriesId, [FromQuery] int limit = 20, CancellationToken cancellationToken = default) {
		if (!PrefixedId.TryToGuid(seriesId, EntityPrefix.Series, out var guid)) {
			return Results.BadRequest("Invalid series ID prefix or format.");
		}

		var records = await auditRepository.GetBySeriesIdAsync(guid, limit, cancellationToken);
		var dtos = records.Select(mapper.ToIngestionAuditDto).ToList();
		return Results.Ok(dtos);
	}
}
