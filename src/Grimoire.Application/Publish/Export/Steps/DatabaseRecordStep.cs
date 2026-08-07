namespace Grimoire.Application.Publish.Export.Steps;

using System;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Publish.Dto;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Entity.Book;

public sealed class DatabaseRecordStep(
	ISeriesExportRecordRepository exportRecords,
	IUnitOfWork unitOfWork) : IExportPipelineStep {
	public int Order => 40;

	public async Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken) {
		if (context.SkipExport || context.AssetId is null || context.ExportResult is null || context.Result is { Success: false }) {
			return;
		}

		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			var formatDir = context.Request.Format.ToString().ToLowerInvariant();
			var prevRecord = await exportRecords.GetBySeriesAndFormatAsync(context.SeriesId, formatDir, cancellationToken);

			if (prevRecord is not null) {
				prevRecord.LastExportedAt = DateTime.UtcNow;
				prevRecord.AssetId = context.AssetId.Value;
				await exportRecords.Update(prevRecord, cancellationToken);
			}
			else {
				await exportRecords.Create(new SeriesExportRecord {
					SeriesId = context.SeriesId,
					Format = formatDir,
					LastExportedAt = DateTime.UtcNow,
					AssetId = context.AssetId.Value
				}, cancellationToken);
			}

			await unitOfWork.CommitTransactionAsync(cancellationToken);
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}

		context.Result = JobResult.Ok(
			context.AssetId.Value.ToString(),
			context.ExportResult.FileName,
			context.ExportResult.ContentType);
	}
}
