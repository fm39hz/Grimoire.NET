namespace Grimoire.Application.Publish.Export;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public sealed class ExportPipeline(
    IEnumerable<IExportPipelineStep> steps,
    ILogger<ExportPipeline> logger) : IExportPipeline
{
    private readonly List<IExportPipelineStep> _steps = steps.OrderBy(s => s.Order).ToList();

    public async Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken)
    {
        foreach (var step in _steps)
        {
            logger.LogInformation("Executing export step: {StepName} (Order={Order})", step.GetType().Name, step.Order);
            await step.ExecuteAsync(context, cancellationToken);
            if (context.Result is { Success: false })
            {
                logger.LogWarning("Export pipeline stopped due to step failure in {StepName}", step.GetType().Name);
                break;
            }
        }
    }
}
