namespace Grimoire.Api.Controller;

using Application.Ingestion.Configuration;
using Application.Ingestion.Research;
using Constant;
using Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/v1/series/{seriesId}/research")]
public sealed class ResearchController(
	IResearchCoordinator coordinator,
	IOptions<IngestionCoreOptions> options) : ControllerBase {
	[HttpGet]
	[ProducesResponseType(typeof(SeriesResearchProfileDto), StatusCodes.Status200OK)]
	public async Task<IResult> Find(string seriesId, CancellationToken cancellationToken) {
		if (!options.Value.Enabled) return Results.NotFound();
		var id = PrefixedId.ToGuid(seriesId, EntityPrefix.Series);
		var profile = await coordinator.Find(id, cancellationToken);
		return profile is null ? Results.NotFound() : Results.Ok(profile);
	}

	[HttpPost]
	[ProducesResponseType(typeof(SeriesResearchProfileDto), StatusCodes.Status200OK)]
	public async Task<IResult> Refresh(string seriesId, [FromQuery] bool force, CancellationToken cancellationToken) {
		if (!options.Value.Enabled || !options.Value.ResearchEnabled) return Results.NotFound();
		var id = PrefixedId.ToGuid(seriesId, EntityPrefix.Series);
		return Results.Ok(await coordinator.Research(id, force, cancellationToken));
	}

	[HttpPatch("evidence")]
	[ProducesResponseType(typeof(SeriesResearchProfileDto), StatusCodes.Status200OK)]
	public async Task<IResult> Confirm(
		string seriesId,
		[FromBody] ConfirmResearchEvidenceDto request,
		CancellationToken cancellationToken) {
		if (!options.Value.Enabled) return Results.NotFound();
		var id = PrefixedId.ToGuid(seriesId, EntityPrefix.Series);
		return Results.Ok(await coordinator.Confirm(id, request, cancellationToken));
	}

	[HttpGet("discovered-sources")]
	[ProducesResponseType(typeof(IReadOnlyList<DiscoveredSourceDto>), StatusCodes.Status200OK)]
	public async Task<IResult> DiscoveredSources(string seriesId, CancellationToken cancellationToken) {
		if (!options.Value.Enabled) return Results.NotFound();
		var id = PrefixedId.ToGuid(seriesId, EntityPrefix.Series);
		var profile = await coordinator.Find(id, cancellationToken);
		return profile is null ? Results.NotFound() : Results.Ok(profile.DiscoveredSources);
	}
}
