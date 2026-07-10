namespace Grimoire.Application.Service.Pipeline.Ingestion;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Domain.Common.Repository;
using Grimoire.Domain.Entity.Book;

/// <summary>
///     Orchestrator that executes the Ingestion pipeline steps transactionally and logs audit history
/// </summary>
public sealed class IngestionCoordinator(
	IEnumerable<IIngestionPipelineStep> steps,
	IUnitOfWork unitOfWork,
	IVolumeRepository volumeRepository,
	IIngestionAuditRepository auditRepository) {

	public async Task ExecuteAsync(IngestionContext context, CancellationToken cancellationToken = default) {
		var orderedSteps = steps.OrderBy(s => s.ExecutionOrder).ToList();

		var volume = await volumeRepository.FindOne(context.VolumeId, cancellationToken) ??
			throw new InvalidOperationException($"Volume with ID {context.VolumeId} not found");
		var seriesId = volume.Path.GetSeriesId();

		var startedAt = DateTimeOffset.UtcNow;

		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			foreach (var step in orderedSteps) {
				await step.ExecuteAsync(context, cancellationToken);
			}
			await unitOfWork.SaveChangesAsync(cancellationToken);
			await unitOfWork.CommitTransactionAsync(cancellationToken);

			// Log successful ingestion audit
			var auditRecord = new IngestionAuditRecord {
				SeriesId = seriesId,
				SourceType = context.SourceType,
				Status = "Success",
				StartedAt = startedAt,
				CompletedAt = DateTimeOffset.UtcNow,
				Summary = $"{context.Segments.Count} segments processed."
			};
			await auditRepository.Create(auditRecord, cancellationToken);
			context.AuditRecordId = auditRecord.Id;
		}
		catch (Exception ex) {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);

			// Log failed ingestion audit
			var auditRecord = new IngestionAuditRecord {
				SeriesId = seriesId,
				SourceType = context.SourceType,
				Status = "Failed",
				StartedAt = startedAt,
				CompletedAt = DateTimeOffset.UtcNow,
				ErrorMessage = ex.Message
			};
			await auditRepository.Create(auditRecord, cancellationToken);
			context.AuditRecordId = auditRecord.Id;
			throw;
		}
	}
}
