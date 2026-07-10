namespace Grimoire.Application.Service.Pipeline.Publishing;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Service.Strategy;

/// <summary>
///     Orchestrator that executes the Publishing pipeline steps sequentially
/// </summary>
public sealed class PublishingCoordinator(IEnumerable<IPublishingPipelineStep> steps) {
	public async Task<ExportResult> ExecuteAsync(PublishingContext context, CancellationToken cancellationToken = default) {
		var orderedSteps = steps.OrderBy(s => s.ExecutionOrder).ToList();

		foreach (var step in orderedSteps) {
			await step.ExecuteAsync(context, cancellationToken);
		}

		return context.Result;
	}
}
