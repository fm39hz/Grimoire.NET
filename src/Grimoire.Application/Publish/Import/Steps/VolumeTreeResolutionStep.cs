namespace Grimoire.Application.Publish.Import.Steps;

using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Import;

public sealed class VolumeTreeResolutionStep(
    IVolumeTreeResolver volumeResolver) : IImportPipelineStep
{
    public int Order => 40;

    public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.Series is null) return;

        context.ResolvedVolumes = await volumeResolver.ResolveAsync(
            context.Series.Id, context.MergedVolumes, cancellationToken);

        context.OnProgress?.Invoke(45);
    }
}
