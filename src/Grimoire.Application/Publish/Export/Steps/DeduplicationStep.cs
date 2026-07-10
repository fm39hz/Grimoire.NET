namespace Grimoire.Application.Publish.Export.Steps;

using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Publish.Dto;
using Grimoire.Domain.Common.Repository;
using Microsoft.Extensions.Logging;

public sealed partial class DeduplicationStep(
	ISeriesExportRecordRepository exportRecords,
	ILogger<DeduplicationStep> logger) : IExportPipelineStep {
	public int Order => 10;

	public async Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken) {
		var formatDir = context.Request.Format.ToString().ToLowerInvariant();
		var prevRecord = await exportRecords.GetBySeriesAndFormatAsync(context.SeriesId, formatDir, cancellationToken);

		if (prevRecord is not null) {
			var maxContentDt = await exportRecords.GetMaxContentTimestampAsync(context.SeriesId, cancellationToken);
			if (prevRecord.LastExportedAt >= maxContentDt) {
				LogExportSkipped(logger, context.JobId, context.SeriesId, formatDir);

				context.SkipExport = true;
				context.AssetId = prevRecord.AssetId;
				context.Result = JobResult.Ok(prevRecord.AssetId.ToString(), "", "");
			}
		}
	}

	[LoggerMessage(LogLevel.Information, "Export skipped (unchanged) — JobId={JobId}, SeriesId={SeriesId}, Format={Format}")]
	private static partial void LogExportSkipped(ILogger logger, string? jobId, Guid seriesId, string format);
}
