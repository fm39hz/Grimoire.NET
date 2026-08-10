namespace Grimoire.Application.Publish.Import.Steps;

using Ingestion.Legacy;

public sealed class LegacyImportShadowOutcomeStep(ILegacyShadowAnalyzer shadowAnalyzer) : IImportPipelineStep {
	public int Order => 70;

	public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken) =>
		await shadowAnalyzer.RecordOutcome(context.ShadowImportRunId, new {
			context.ChaptersCreated,
			context.ChaptersUpdated,
			VolumesCreated = context.ResolvedVolumes.Count(static volume => volume.WasCreated),
			VolumesReused = context.ResolvedVolumes.Count(static volume => !volume.WasCreated),
			SeriesId = context.Series?.Id
		}, cancellationToken);
}
