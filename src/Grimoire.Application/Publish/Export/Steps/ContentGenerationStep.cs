namespace Grimoire.Application.Publish.Export.Steps;

using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Publish.Dto;
using Grimoire.Application.Service.Contract;

public sealed class ContentGenerationStep(
	IBinderyService bindery) : IExportPipelineStep {
	public int Order => 20;

	public async Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken) {
		if (context.SkipExport) {
			return;
		}

		var exportResult = await bindery.ExportSeriesAsync(context.SeriesId, context.Request, cancellationToken);
		if (!exportResult.Success) {
			context.Result = JobResult.Fail(exportResult.ErrorMessage ?? "Export generation failed");
			return;
		}

		context.ExportResult = exportResult;
	}
}
