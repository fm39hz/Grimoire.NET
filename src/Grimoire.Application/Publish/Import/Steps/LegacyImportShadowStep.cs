namespace Grimoire.Application.Publish.Import.Steps;

using Ingestion.Legacy;

/// <summary>Analyzes the normalized EPUB before legacy volume/chapter mutation.</summary>
public sealed class LegacyImportShadowStep(
	ILegacySourcePackageAdapter adapter,
	ILegacyShadowAnalyzer shadowAnalyzer) : IImportPipelineStep {
	public int Order => 25;

	public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken) {
		if (context.Series is null || context.Normalized is null) return;
		var package = adapter.FromEpub(context.Series.Id, context.Normalized, context.MergedVolumes, context.JobId);
		context.ShadowImportRunId = await shadowAnalyzer.Analyze(package, cancellationToken);
	}
}
