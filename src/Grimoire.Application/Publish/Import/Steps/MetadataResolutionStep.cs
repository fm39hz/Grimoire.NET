namespace Grimoire.Application.Publish.Import.Steps;

using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Service.Contract;

public sealed class MetadataResolutionStep(
    ISeriesService seriesService) : IImportPipelineStep
{
    public int Order => 20;

    public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken)
    {
        var (series, _) = await seriesService.GetOrCreate(context.SeriesDto, cancellationToken);
        context.Series = series;
        context.OnProgress?.Invoke(5);
    }
}
