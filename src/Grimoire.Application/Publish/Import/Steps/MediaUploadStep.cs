namespace Grimoire.Application.Publish.Import.Steps;

using System;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Import;

public sealed class MediaUploadStep(
    IMediaImportService mediaService) : IImportPipelineStep
{
    public int Order => 30;

    public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.Series is null || context.Normalized is null) return;

        var seriesId = context.Series.Id;
        var prefix = $"staging/import/{seriesId}";
        context.FileMap = await mediaService.UploadFilesAsync(context.Normalized.Files, seriesId, prefix, cancellationToken);

        if (context.Normalized.Cover is not null)
            await mediaService.UploadCoverAsync(context.SeriesDto, seriesId, context.Normalized.Cover, cancellationToken);
    }
}
