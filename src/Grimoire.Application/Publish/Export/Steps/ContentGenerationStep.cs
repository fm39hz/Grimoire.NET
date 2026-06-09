namespace Grimoire.Application.Publish.Export.Steps;

using System;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Service.Contract;
using Grimoire.Application.Publish.Dto;

public sealed class ContentGenerationStep(
    IBinderyService bindery) : IExportPipelineStep
{
    public int Order => 20;

    public async Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.SkipExport) return;

        var exportResult = await bindery.ExportSeriesAsync(context.SeriesId, context.Request, cancellationToken);
        if (!exportResult.Success)
        {
            context.Result = JobResult.Fail(exportResult.ErrorMessage ?? "Export generation failed");
            return;
        }

        context.ExportResult = exportResult;
    }
}
