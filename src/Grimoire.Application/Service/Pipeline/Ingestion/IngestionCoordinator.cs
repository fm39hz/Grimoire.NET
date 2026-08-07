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

		// The caller resolves the series id once per sync/import run; for single-chapter creates the
		// service populates it from the owning volume. Falls back to a volume lookup when absent.
		var seriesId = context.SeriesId ?? await ResolveSeriesIdAsync(context, cancellationToken);

		var startedAt = DateTimeOffset.UtcNow;

		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			foreach (var step in orderedSteps) {
				await step.ExecuteAsync(context, cancellationToken);
			}

			// Audit record is created inside the same transaction as the chapter + segments, so the
			// chapter and its audit trail commit (or roll back) atomically — no separate post-commit
			// INSERT that could be lost on a crash, and one less round-trip per chapter.
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

			await unitOfWork.SaveChangesAsync(cancellationToken);
			await unitOfWork.CommitTransactionAsync(cancellationToken);
		}
		catch (Exception ex) {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);

			// Failure audit is written after the rollback — it must survive even though the
			// chapter transaction was undone.
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

	private async Task<Guid> ResolveSeriesIdAsync(IngestionContext context, CancellationToken cancellationToken) {
		var volume = await volumeRepository.FindOne(context.VolumeId, cancellationToken) ??
			throw new InvalidOperationException($"Volume with ID {context.VolumeId} not found");
		return volume.Path.GetSeriesId();
	}
}
