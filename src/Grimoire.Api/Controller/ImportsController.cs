namespace Grimoire.Api.Controller;

using Application.Ingestion.Analysis;
using Application.Ingestion.Configuration;
using Application.Ingestion.Contract;
using Application.Ingestion.Execution;
using Constant;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[ApiController]
[Route(RouteConstant.CONTROLLER)]
public sealed class ImportsController(
	IImportAnalysisService analysisService,
	IImportExecutionService executionService,
	IOptions<IngestionCoreOptions> options) : ControllerBase {
	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<ImportRunResponseDto>), StatusCodes.Status200OK)]
	public async Task<IResult> List([FromQuery] int limit, CancellationToken cancellationToken) {
		if (!options.Value.Enabled) return Results.NotFound();
		return Results.Ok(await analysisService.List(limit <= 0 ? 50 : limit, cancellationToken));
	}

	[HttpPost]
	[ProducesResponseType(typeof(ImportRunResponseDto), StatusCodes.Status202Accepted)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IResult> Analyze([FromBody] SourcePackageDto package, CancellationToken cancellationToken) {
		if (!options.Value.Enabled) return Results.NotFound();

		var run = await analysisService.Analyze(package, cancellationToken);
		return Results.Accepted($"/api/v1/imports/{run.Id}", run);
	}

	[HttpGet("{id}")]
	[ProducesResponseType(typeof(ImportRunResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IResult> Find(string id, CancellationToken cancellationToken) {
		if (!options.Value.Enabled || !TryParseId(id, out var guid)) return Results.NotFound();

		var run = await analysisService.Find(guid, cancellationToken);
		return run is null ? Results.NotFound() : Results.Ok(run);
	}

	[HttpGet("{id}/plan")]
	[ProducesResponseType(typeof(ReconciliationPlanDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IResult> FindPlan(string id, CancellationToken cancellationToken) {
		if (!options.Value.Enabled || !TryParseId(id, out var guid)) return Results.NotFound();

		var run = await analysisService.Find(guid, cancellationToken);
		return run?.Plan is null ? Results.NotFound() : Results.Ok(run.Plan);
	}

	[HttpPatch("{id}/decisions")]
	[ProducesResponseType(typeof(ImportRunResponseDto), StatusCodes.Status200OK)]
	public async Task<IResult> SaveDecisions(
		string id,
		[FromBody] UpdateImportDecisionsDto request,
		CancellationToken cancellationToken) {
		if (!options.Value.Enabled || !TryParseId(id, out var guid)) return Results.NotFound();
		return Results.Ok(await executionService.SaveDecisions(guid, request, cancellationToken));
	}

	[HttpPost("{id}/commit")]
	[ProducesResponseType(typeof(ImportRunResponseDto), StatusCodes.Status200OK)]
	public async Task<IResult> Commit(string id, CancellationToken cancellationToken) {
		if (!options.Value.Enabled || !TryParseId(id, out var guid)) return Results.NotFound();
		return Results.Ok(await executionService.Commit(guid, safeOnly: false, cancellationToken));
	}

	[HttpPost("{id}/commit-safe")]
	[ProducesResponseType(typeof(ImportRunResponseDto), StatusCodes.Status200OK)]
	public async Task<IResult> CommitSafe(string id, CancellationToken cancellationToken) {
		if (!options.Value.Enabled || !TryParseId(id, out var guid)) return Results.NotFound();
		return Results.Ok(await executionService.Commit(guid, safeOnly: true, cancellationToken));
	}

	private static bool TryParseId(string value, out Guid id) {
		id = Guid.Empty;
		return value.StartsWith("imp_", StringComparison.Ordinal) && Guid.TryParse(value[4..], out id);
	}
}
