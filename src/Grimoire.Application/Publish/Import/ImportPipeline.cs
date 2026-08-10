namespace Grimoire.Application.Publish.Import;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Domain.Common.Repository;
using Microsoft.Extensions.Logging;
using Service.Contract;

public sealed partial class ImportPipeline(
	IEnumerable<IImportPipelineStep> steps,
	IUnitOfWork unitOfWork,
	ILogger<ImportPipeline> logger,
	ISeriesRevisionService revisionService) : IImportPipeline {
	private readonly List<IImportPipelineStep> _steps = [.. steps.OrderBy(static s => s.Order)];

	public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken) {
		using var revisionBatch = revisionService.Suppress();
		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			foreach (var step in _steps) {
				var stageName = step.GetType().Name.Replace("Step", "");
				context.CurrentStage = stageName;
				context.ReportSubProgress(0.0);
				LogExecutingImportStep(logger, step.GetType().Name, step.Order);
				await step.ExecuteAsync(context, cancellationToken);

				if (context.Result is { Success: false }) {
					LogImportPipelineStopped(logger, step.GetType().Name);
					await unitOfWork.RollbackTransactionAsync(cancellationToken);
					return;
				}
			}

			await unitOfWork.CommitTransactionAsync(cancellationToken);
		}
		catch (Exception ex) {
			LogImportPipelineCrashed(logger, ex);
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}

	[LoggerMessage(LogLevel.Information, "Executing import step: {StepName} (Order={Order})")]
	private static partial void LogExecutingImportStep(ILogger logger, string stepName, int order);

	[LoggerMessage(LogLevel.Warning, "Import pipeline stopped due to step failure in {StepName}")]
	private static partial void LogImportPipelineStopped(ILogger logger, string stepName);

	[LoggerMessage(LogLevel.Error, "Import pipeline crashed, rolling back transaction")]
	private static partial void LogImportPipelineCrashed(ILogger logger, Exception ex);
}
