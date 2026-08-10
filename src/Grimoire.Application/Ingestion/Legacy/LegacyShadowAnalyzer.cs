namespace Grimoire.Application.Ingestion.Legacy;

using System.Text.Json;
using Analysis;
using Configuration;
using Contract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class LegacyShadowAnalyzer(
	IImportAnalysisService analysisService,
	IOptions<IngestionCoreOptions> options,
	ILogger<LegacyShadowAnalyzer> logger) : ILegacyShadowAnalyzer {
	public async Task<Guid?> Analyze(SourcePackageDto package, CancellationToken cancellationToken = default) {
		if (!options.Value.Enabled || !options.Value.ShadowMode) return null;
		try {
			var result = await analysisService.Analyze(package, cancellationToken);
			return Guid.Parse(result.Id[4..]);
		}
		catch (Exception exception) {
			logger.LogWarning(exception, "Shadow reconciliation failed for {ProducerId}; legacy flow will continue.", package.Producer.Id);
			return null;
		}
	}

	public async Task RecordOutcome(Guid? importRunId, object outcome, CancellationToken cancellationToken = default) {
		if (importRunId is null) return;
		try {
			await analysisService.RecordLegacyOutcome(importRunId.Value, JsonSerializer.Serialize(outcome), cancellationToken);
		}
		catch (Exception exception) {
			logger.LogWarning(exception, "Could not record legacy outcome for shadow import {ImportRunId}.", importRunId);
		}
	}
}
