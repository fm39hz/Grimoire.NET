namespace Grimoire.Application.Publish.Export;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public sealed partial class ExportPipeline(
	IEnumerable<IExportPipelineStep> steps,
	ILogger<ExportPipeline> logger) : IExportPipeline {
	private readonly List<IExportPipelineStep> _steps = [.. steps.OrderBy(static s => s.Order)];

	public async Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken) {
		foreach (var step in _steps) {
			var stageName = step.GetType().Name.Replace("Step", "");
			context.CurrentStage = stageName;
			context.ReportSubProgress(0.0);
			LogExecutingExportStep(logger, step.GetType().Name, step.Order);
			await step.ExecuteAsync(context, cancellationToken);
			if (context.Result is { Success: false }) {
				LogExportPipelineStopped(logger, step.GetType().Name);
				break;
			}
		}
	}

	[LoggerMessage(LogLevel.Information, "Executing export step: {StepName} (Order={Order})")]
	private static partial void LogExecutingExportStep(ILogger logger, string stepName, int order);

	[LoggerMessage(LogLevel.Warning, "Export pipeline stopped due to step failure in {StepName}")]
	private static partial void LogExportPipelineStopped(ILogger logger, string stepName);
}
