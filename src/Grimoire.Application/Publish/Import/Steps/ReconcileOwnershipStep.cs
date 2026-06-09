namespace Grimoire.Application.Publish.Import.Steps;

using System;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Service.Contract;
using Grimoire.Application.Publish.Dto;

public sealed class ReconcileOwnershipStep(
    IAssetOwnershipService assetOwnershipService) : IImportPipelineStep
{
    public int Order => 60;

    public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.Series is null) return;

        await assetOwnershipService.ReconcileSeriesAsync(context.Series.Id, cancellationToken);

        context.Result = JobResult.Ok(
            context.Series.Id.ToString(), 
            "import-completed", 
            "application/json");
    }
}
