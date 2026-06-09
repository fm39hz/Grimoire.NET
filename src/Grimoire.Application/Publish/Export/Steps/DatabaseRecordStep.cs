namespace Grimoire.Application.Publish.Export.Steps;

using System;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Entity.Book;
using Grimoire.Application.Publish.Dto;

public sealed class DatabaseRecordStep(
    ISeriesExportRecordRepository exportRecords) : IExportPipelineStep
{
    public int Order => 40;

    public async Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken)
    {
        if (context.SkipExport || context.AssetId is null || context.ExportResult is null || context.Result is { Success: false }) return;

        var formatDir = context.Request.Format.ToString().ToLowerInvariant();
        var prevRecord = await exportRecords.GetBySeriesAndFormatAsync(context.SeriesId, formatDir, cancellationToken);

        if (prevRecord is not null)
        {
            prevRecord.LastExportedAt = DateTime.UtcNow;
            prevRecord.AssetId = context.AssetId.Value;
            await exportRecords.Update(prevRecord, cancellationToken);
        }
        else
        {
            await exportRecords.Create(new SeriesExportRecord
            {
                SeriesId = context.SeriesId,
                Format = formatDir,
                LastExportedAt = DateTime.UtcNow,
                AssetId = context.AssetId.Value
            }, cancellationToken);
        }

        context.Result = JobResult.Ok(
            context.AssetId.Value.ToString(), 
            context.ExportResult.FileName, 
            context.ExportResult.ContentType);
    }
}
