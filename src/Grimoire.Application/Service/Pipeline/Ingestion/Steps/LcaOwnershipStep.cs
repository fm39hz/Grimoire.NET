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

		var seriesId = context.Chapter.Path.GetSeriesId();
		if (seriesId != Guid.Empty) {
			await assetOwnershipService.ReconcileSeriesAsync(seriesId, cancellationToken);
		}
	}
}
