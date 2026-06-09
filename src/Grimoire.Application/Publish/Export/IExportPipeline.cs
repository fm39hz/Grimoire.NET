namespace Grimoire.Application.Publish.Export;

using System.Threading;
using System.Threading.Tasks;

public interface IExportPipeline
{
    Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken);
}
