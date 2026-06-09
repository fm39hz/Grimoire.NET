namespace Grimoire.Application.Publish.Export.Steps;

using System;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Domain.Common.Repository;
using Grimoire.Application.Publish.Dto;
using Microsoft.Extensions.Logging;

public sealed class DeduplicationStep(
    ISeriesExportRecordRepository exportRecords,
    ILogger<DeduplicationStep> logger) : IExportPipelineStep
{
    public int Order => 10;

    public async Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken)
    {
        var formatDir = context.Request.Format.ToString().ToLowerInvariant();
        var prevRecord = await exportRecords.GetBySeriesAndFormatAsync(context.SeriesId, formatDir, cancellationToken);
        
        if (prevRecord is not null)
        {
            var maxContentDt = await exportRecords.GetMaxContentTimestampAsync(context.SeriesId, cancellationToken);
            if (prevRecord.LastExportedAt >= maxContentDt)
            {
                logger.LogInformation(
                    "Export skipped (unchanged) — JobId={JobId}, SeriesId={SeriesId}, Format={Format}",
                    context.JobId, context.SeriesId, formatDir);
                
                context.SkipExport = true;
                context.AssetId = prevRecord.AssetId;
                context.Result = JobResult.Ok(prevRecord.AssetId.ToString(), "", "");
            }
        }
    }
}
