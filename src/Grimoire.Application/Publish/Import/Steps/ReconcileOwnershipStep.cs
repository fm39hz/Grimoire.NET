namespace Grimoire.Application.Publish.Import.Steps;

using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Publish.Dto;
using Grimoire.Application.Service.Contract;
using Grimoire.Domain.Common.Repository;

public sealed class ReconcileOwnershipStep(
	IAssetOwnershipService assetOwnershipService,
	ISeriesRepository seriesRepository) : IImportPipelineStep {
	public int Order => 60;

	public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken) {
		if (context.Series is null) {
			return;
		}

		await assetOwnershipService.ReconcileSeriesAsync(context.Series.Id, cancellationToken);
		var series = await seriesRepository.FindOneTracked(context.Series.Id, cancellationToken) ?? context.Series;
		series.AdvanceRevision(series.Revision);
		await seriesRepository.Update(series, cancellationToken);

		context.ReportSubProgress(1.0);

		context.Result = JobResult.Ok(
			context.Series.Id.ToString(),
			"import-completed",
			"application/json");
	}
}
