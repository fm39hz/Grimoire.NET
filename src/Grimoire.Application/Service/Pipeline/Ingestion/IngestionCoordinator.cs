namespace Grimoire.Application.Service.Pipeline.Ingestion;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Domain.Common.Repository;

/// <summary>
///     Orchestrator that executes the Ingestion pipeline steps transactionally
/// </summary>
public sealed class IngestionCoordinator(
	IEnumerable<IIngestionPipelineStep> steps,
	IUnitOfWork unitOfWork) {

	public async Task ExecuteAsync(IngestionContext context, CancellationToken cancellationToken = default) {
		var orderedSteps = steps.OrderBy(s => s.ExecutionOrder).ToList();

		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			foreach (var step in orderedSteps) {
				await step.ExecuteAsync(context, cancellationToken);
			}
			await unitOfWork.SaveChangesAsync(cancellationToken);
			await unitOfWork.CommitTransactionAsync(cancellationToken);
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}
}
