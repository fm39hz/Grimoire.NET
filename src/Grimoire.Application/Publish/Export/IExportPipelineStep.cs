namespace Grimoire.Application.Publish.Export;

using System.Threading;
using System.Threading.Tasks;

public interface IExportPipelineStep
{
    int Order { get; }
    Task ExecuteAsync(ExportPipelineContext context, CancellationToken cancellationToken);
}
