namespace Grimoire.Application.Publish.Import;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Domain.Common.Repository;
using Microsoft.Extensions.Logging;

public sealed class ImportPipeline(
    IEnumerable<IImportPipelineStep> steps,
    IUnitOfWork unitOfWork,
    ILogger<ImportPipeline> logger) : IImportPipeline
{
    private readonly List<IImportPipelineStep> _steps = steps.OrderBy(s => s.Order).ToList();

    public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var step in _steps)
            {
                logger.LogInformation("Executing import step: {StepName} (Order={Order})", step.GetType().Name, step.Order);
                await step.ExecuteAsync(context, cancellationToken);
                
                if (context.Result is { Success: false })
                {
                    logger.LogWarning("Import pipeline stopped due to step failure in {StepName}", step.GetType().Name);
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return;
                }
            }

            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import pipeline crashed, rolling back transaction");
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
