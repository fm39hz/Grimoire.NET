namespace Grimoire.Application.Service.Pipeline.Ingestion.Steps;

using System;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Service.Contract;

public sealed class LcaOwnershipStep(IAssetOwnershipService assetOwnershipService) : IIngestionPipelineStep {
	public int ExecutionOrder => 30; // Runs after PersistenceStep (ExecutionOrder = 20)

	public async Task ExecuteAsync(IngestionContext context, CancellationToken cancellationToken) {
		if (context.Chapter?.Path == null) {
			return;
		}

		// During a bulk import, ownership is reconciled once for the whole series at the end
		// (ReconcileOwnershipStep in the publish pipeline), not per chapter. Reconciling here per
		// chapter would re-scan the full series each time — O(chapters²) — with no final-result
		// difference (the ownership LCA is monotonic as usages accumulate).
		if (context.IsBulkImport) {
			return;
		}

		var seriesId = context.Chapter.Path.GetSeriesId();
		if (seriesId != Guid.Empty) {
			await assetOwnershipService.ReconcileSeriesAsync(seriesId, cancellationToken);
		}
	}
}
