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
        if (context.Series is null || context.Normalized is null || context.SeriesDto is null) return;

        var seriesId = context.Series.Id;
        var prefix = $"staging/import/{seriesId}";

        var totalFiles = context.Normalized.Files.Count;
        context.FileMap = await mediaService.UploadFilesAsync(
            context.Normalized.Files,
            seriesId,
            prefix,
            onProgress: uploadedCount =>
            {
                if (totalFiles > 0)
                {
                    context.ReportSubProgress((double)uploadedCount / totalFiles);
                }
            },
            cancellationToken);

        if (context.Normalized.Cover is not null)
        {
            await mediaService.UploadCoverAsync(context.SeriesDto, seriesId, context.Normalized.Cover, cancellationToken);
        }

        context.ReportSubProgress(1.0);
    }
}
